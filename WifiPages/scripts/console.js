// Constants & Singletons
var TRACE = "trace"; // Trace messages CSS class
var MSG = "console"; // Console messages CSS class
var CSSINDEX = -1; // the last, i.e. the embedded style sheet
var ID = {
  OPTIONS:'options',
  LOGIN:'login',
  SIMULATORS:'simulators',
  MESSAGES:'messages',
  TABULATORS:'tabulators',
  DEFAULT_CONSOLE:'rootconsole',
  COMMAND:'command'
}
var NAME = {
  CREDENTIALS:'credentials',
  SETTINGS:'settings',
  DEBUG:'debug',
  TEXT:'text',
  MONOSPACE:'mono',
  TRACING:'trace',
  BUFSIZE:'bufsize'
}
var NO_CONNECTION;
var Colors = new Palette(['orange', 'yellow', 'aqua', 'fuchsia', 'gray', 'lime', 'red', 'silver', 'teal']);
var loginAreaRule;
var outputAreaRule;
// Global variables
var wifi = false;
var messageArea = null;
var settings = null; // Ref to settings form
var consoles = new Object();
var activeConsole;
var simulatorNode;
var simulators;

// Initialize
function DoOnload() {
  // Hide JavaScript warning
  document.getElementById('noscript').style.display = 'none';
  document.getElementById('console').style.display = 'block';  
  // Set up CSS
  outputAreaRule = AddCSSRule(CSSINDEX, '#console #command');
  outputAreaRule.style.display = 'none';
  loginAreaRule = AddCSSRule(CSSINDEX, '#console #login');
  loginAreaRule.style.display = 'none';
  logMessageRule = AddCSSRule(CSSINDEX, '#console #messages .' + TRACE);
  // Define the default console that connects to the Wifi server
  activeConsole = new Console(ID.DEFAULT_CONSOLE);
  activeConsole.rule = AddCSSRule(CSSINDEX, '#console #messages .' + ID.DEFAULT_CONSOLE);
  activeConsole.rule.style.display = 'block';
  consoles[activeConsole.name] = activeConsole;
  consoles.count = 1;
  document.getElementById(ID.DEFAULT_CONSOLE).getElementsByTagName('span')[0].style.display = 'none';
  document.getElementById(ID.LOGIN).getElementsByTagName('div')[0].style.display = 'none';
  // Amend event handlers of some menu options
  var options = document.getElementById(ID.OPTIONS).getElementsByTagName('li');
  options[0].onclick = AmendCheckboxOptionEvent(document.getElementsByName(NAME.MONOSPACE)[0]);
  options[1].onclick = AmendCheckboxOptionEvent(document.getElementsByName(NAME.TRACING)[0]);
  options[2].onclick = AmendCheckboxOptionEvent(document.getElementsByName(NAME.TRACING)[1]);
  options[3].onclick = CreateEventHandler(function(e) {
      if (e.target != document.getElementsByName(NAME.BUFSIZE)[0])
        e.target.firstChild.select();
    }
  );
  // Initialize other globals
  NO_CONNECTION = document.getElementsByName(NAME.TEXT)[0].value;
  settings = document.forms[NAME.SETTINGS];
  messageArea = document.getElementById(ID.MESSAGES);
  DebugMode(settings[NAME.DEBUG].value);
  // Get default parameters from form
  ChangedOption(NAME.TRACING);
  ChangedOption(NAME.MONOSPACE);
  // Set up login area
  document.forms[NAME.CREDENTIALS].address.value = location.host;
  if (document.forms[NAME.CREDENTIALS].user.value && document.forms[NAME.CREDENTIALS].password.value)
    loginAreaRule.style.display = 'block';
  else {
    // With Wifi, get credentials and connect automatically
    wifi = true;
    var buttons = document.getElementById(ID.COMMAND).getElementsByTagName('input');
    buttons[buttons.length-1].style.display = 'none';
    buttons[buttons.length-2].style.display = 'none';
    simulatorNode = document.getElementById(ID.SIMULATORS).cloneNode(true);
    // Retrieve console login info
    var query = location.protocol.concat('//', location.host, location.pathname, 'data/console/', location.search);
    AjaxSend(query, null,
      function(xml) {
        var element = xml.getElementsByTagName('Console')[0];
        document.forms[NAME.CREDENTIALS].user.value = element.getAttribute('User');
        document.forms[NAME.CREDENTIALS].password.value = element.getAttribute('Password');
        Connect();
      }
    );
  }
}
// Terminate
function DoOnunload() {
  for (var name in consoles)
    if (consoles[name].sessionId)
      CloseSession(consoles[name]);
}

// The console struct
function Console(identifier) {
  this.name = identifier;
  this.serviceURL = null;
  this.user = null;
  this.password = null;
  this.prompt = null;
  this.consoleRequest = null;
  this.sessionId = null;
  this.helpNode = null;
  this.rule = null; // CSS rule
}

function Connect() {
  var address = document.forms[NAME.CREDENTIALS].address.value;
  if (!address) {
    if (simulators.length > 1) {
      for (var i = 0; i < document.forms[NAME.CREDENTIALS].simulator.length; ++i)
        if (document.forms[NAME.CREDENTIALS].simulator[i].checked) {
          address = document.forms[NAME.CREDENTIALS].simulator[i].value;
          break;
        }
    }
    else
      address = document.forms[NAME.CREDENTIALS].simulator.value;
  }
  activeConsole.serviceURL = "http://" + address;
  activeConsole.user = document.forms[NAME.CREDENTIALS].user.value;
  activeConsole.password = document.forms[NAME.CREDENTIALS].password.value;
  StartSession(activeConsole);
}
function Disconnect() {
  var console = activeConsole;
  if (console.name != ID.DEFAULT_CONSOLE)
    SwitchConsole(ID.DEFAULT_CONSOLE);
  CloseSession(console);
  if (console.name != ID.DEFAULT_CONSOLE) {
    // Remove tab
    var tab = document.getElementById(console.name);
    document.getElementById(ID.TABULATORS).removeChild(tab);
  }
}
function ToggleConsole(visible) {
  loginAreaRule.style.display = (visible ? 'none' : 'block'); // login area
  outputAreaRule.style.display = (visible ? 'block' : 'none'); // console area
  messageArea.style.display = (visible ? 'block' : 'none');
  if (visible) {
    activeConsole.rule.style.display = 'block';
    ScrollBottom.Restore(activeConsole, messageArea);
  }
}

// Remote console requests
function StartSession(console) {
  try {
    console.consoleRequest = AjaxSend(console.serviceURL.concat('/StartSession/'),
      'USER=' + console.user + '&PASS=' + console.password,
      function(xml, status) {
        ShowStatus(console, status);
        if (xml && status.code == 200) {
          ToggleConsole(true);
          // Process StartSession response
          Output(TRACE, "[StartSession:".concat(console.name, "] Processing response"));
          var elements = xml.getElementsByTagName('SessionID');
          if (elements && elements[0].nodeType == 1)
            console.sessionId = elements[0].firstChild.nodeValue;
          SetPrompt(console, xml.getElementsByTagName('Prompt')[0].firstChild.nodeValue);
          console.helpNode = xml.getElementsByTagName('HelpTree')[0];
          Output(TRACE, "[StartSession:".concat(console.name, "] SessionId=" + console.sessionId + " Prompt='" + console.prompt + "' Help entries:" + console.helpNode.childNodes.length));
          // Get console output
          void AjaxSend(console.serviceURL.concat('/ReadResponses/', console.sessionId, '/'), '',
            function(xml, status) { ReadResponses(console, xml, status); },
            console.consoleRequest);
        }
      }
    );
  }
  catch (e) {
    NoConnection(console);
  }
}
function CloseSession(console) {
  Output(TRACE, "[CloseSession:".concat(console.name, "] " + (console.sessionId ? "Closing session" : "No active session")));
  if (console.sessionId) {
    try {
      void AjaxSend(console.serviceURL.concat('/CloseSession/'), 'ID=' + console.sessionId,
        function() {
          NoConnection(console);
          if (console.name == ID.DEFAULT_CONSOLE && !wifi)
            ToggleConsole(false);
        },
        console.consoleRequest
      );
      // TODO: Use timeout to detect unavailable server
    }
    catch (e) {
      NoConnection(console);
    }
  }
}
function ReadResponses(console, xml, status) {
  ShowStatus(console, status);
  if (xml) {
    Output(TRACE, "[ReadResponses:".concat(console.name, "] Processing response"));
    elements = xml.getElementsByTagName('Line');
    if (elements) {
      // 153:normal:19:18:39 - [MODULES]: [XmlRpcRouterModule]: Initializing.
      var pattern = /(\d{2}:\d{2}:\d{2}\s*-\s*\[)([\w\s!]*)(\]:*\s*)(.*)$/;
      ScrollBottom.Save(console, messageArea);
      UpdateScrollback(elements.length);
      for (var i = 0; i < elements.length; ++i) {
        var lineNode = elements[i];
        lineNode.removeAttribute('Number');
        var parts = lineNode.firstChild.nodeValue.split(':');
        parts.shift(); // discard line number
        var level = parts.shift();
        var line = parts.join(':');
        if (line.substr(1, 2) == '++') {
          SetPrompt(console, line.substr(3));
          UpdateScrollback(1); Output(TRACE, "[ReadResponses:".concat(console.name, "] '", line, "'"), true);
          continue;
        }
        else if (line.length >= 1024) {
          UpdateScrollback(1); Output(TRACE, "[ReadResponses:".concat(console.name, "] Long line with ", line.length, " bytes"), true);
        }
        // Format line
        var matches = line.match(pattern);
        if (matches) {
          line = new Array();
          // Timestamp
          line[0] = document.createTextNode(matches[1]);
          // Module name
          line[1] = document.createElement('span');
          line[1].setAttribute('style', 'color:' + Colors.GetForString(matches[2]));
          line[1].appendChild(document.createTextNode(matches[2]));
          // Colon & whitespace
          line[2] = document.createTextNode(matches[3]);
          // Message
          line[3] = document.createElement('span');
          line[3].setAttribute('class', level);
          line[3].appendChild(document.createTextNode(matches[4]));
        }
        Output(MSG + " " + console.name, line, true);
      }
      ScrollBottom.Restore(console, messageArea);
    }
  }
  if (console.sessionId) {
    try {
      void AjaxSend(console.serviceURL.concat('/ReadResponses/', console.sessionId, '/'), '',
        function(xml, status) { ReadResponses(console, xml, status); },
        console.consoleRequest);
    }
    catch (e) {
      NoConnection(console);
    }          
  }
}
function Command(console, command) {
  command = command.replace(/\s*$/, '');
  if (console.sessionId) {
    Output(TRACE, "[Command:".concat(console.name, "] Sending command '" + command + "'"));
    Output(MSG + " " + console.name, console.prompt + " " + command);
    try {
      void AjaxSend(console.serviceURL.concat('/SessionCommand/'), 'ID=' + console.sessionId + '&COMMAND=' + command,
        function(xml, status) {
          ShowStatus(console, status);
          if (xml) {
            // Process SessionCommand response
            var elements = xml.getElementsByTagName('Result');
            Output(TRACE, "[Command:".concat(console.name, "] Result=" + elements[0].firstChild.nodeValue));
          }
        }
      );
    }
    catch (e) {
      NoConnection(console);
    }
  }    
  else
    Output(TRACE, "[Command:".concat(console.name, "] Not logged in."));
}

// Auxiliary functions
function NoConnection(console) {
  console.sessionId = null;
  if (console.consoleRequest)
    console.consoleRequest.xhr.abort();
  console.consoleRequest = null;
  ShowStatus(console, new XMLHttpStatus(0, NO_CONNECTION));
}
function ShowStatus(console, status) {
  var errorLevel = 'good';
  if (status.code != 200)
    errorLevel = 'error';
  var consoleElement = document.getElementById(console.name);
  if (consoleElement) {
    var node = consoleElement.getElementsByTagName('span')[2];
    node.parentNode.setAttribute('title', "Status: " + status.text);
    node.setAttribute('class', errorLevel);
  }
}  
function SetPrompt(console, newPrompt) {
  console.prompt = newPrompt.replace(/\s*#?\s*$/, " #");
  document.getElementById(console.name).getElementsByTagName('span')[1].firstChild.data = console.prompt.slice(0, -2).replace(/ /g, '\xA0');
}

// Other Form actions
function ChangedOption(name) {
  if (NAME.MONOSPACE == name) {
    // Use monospace or proportional font
    if (settings[name].checked)
      messageArea.setAttribute('class', 'monospace');
    else
      messageArea.removeAttribute('class');
  }
  else if (NAME.TRACING == name) {
    // Hide or show trace message elements
    ScrollBottom.Save(activeConsole, messageArea);
    logMessageRule.style.display = (settings[name][1].checked ? 'none' : 'block');
    ScrollBottom.Restore(activeConsole, messageArea);
  }
  else if (NAME.BUFSIZE == name)
    UpdateScrollback();
}
function UpdateScrollback(addLines) {
  var excessLines = messageArea.childNodes.length + (addLines ? addLines : 0) - settings.bufsize.value;
  while (excessLines-- > 0 && messageArea.childNodes.length)
    messageArea.removeChild(messageArea.firstChild);
}

// AJAX functions
function XMLHttpStatus(code, text) {
  this.code = code;
  this.text = text;
}
function AjaxRequest(callback) {
  this.id = (new Date()).getTime();
  this.callback = callback;
  // Find and create an appropriate XMLHttpRequest object
  this.xhr = null;
  var xhrFactories = [
    function() { return new XMLHttpRequest(); },
    function() { return new ActiveXObject('Msxml2.XMLHTTP'); }
  ];
  for (var factory in xhrFactories) {
    try {
      this.xhr = xhrFactories[factory]();
    }
    catch (e) { continue; }
    break;
  }
}
function AjaxSend(url, postData, callback, request) {
  if (request)
    request.callback = callback;
  else
    request = new AjaxRequest(callback);
  Output(TRACE, "[AJAX:".concat(request.id, "] ", url, " data='", postData, "'"));
  var method = (typeof(postData) == 'string') ? 'POST' : 'GET';
  var async = (request.callback) ? true : false;
  // Set up the request
  try {
    request.xhr.open(method, url, async);
    if (async)
        request.xhr.onreadystatechange = function() { AjaxReceive(request); };
    request.xhr.send(postData);
    if (!async)
      AjaxReceive(request);
    else 
      return request;
  }
  catch (e) {
      Output(TRACE, "[AJAX:".concat(request.id, "] Exception with open: ", e));
      throw e;
  }
  return null;
}
function AjaxReceive(request) {
  if (request.xhr.readyState < 4) {
    Output(TRACE, "[AJAX:".concat(request.id, "] ReadyState=", request.xhr.readyState));
    return;
  }
  var statusText;
  try {
    statusText = request.xhr.statusText;
  }
  catch (e) {
    Output(TRACE, "[AJAX:".concat(request.id, "] Exception with receive: ", e));
  }
  if (!statusText)
    statusText = NO_CONNECTION;
  if (request.xhr.status != 200) // HTTP error
    Output(TRACE, "[AJAX:".concat(request.id, "] HTTP status ", request.xhr.status, ": ", statusText));
  request.callback(request.xhr.responseXML, new XMLHttpStatus(request.xhr.status, statusText));
}

// Auxiliary functions
function Output(cssClass, logObj, noBufferMaintenance) {
  if (cssClass == TRACE && !settings.trace[0].checked)
    return;
  var element = document.createElement('div');
  element.setAttribute('class', cssClass);
  if (typeof(logObj) == 'string')
    element.appendChild(document.createTextNode(logObj));
  else { // assume an array with nodes
    for (var i = 0; i < logObj.length; ++i)
      element.appendChild(logObj[i]);
  }
  if (!noBufferMaintenance) {
    ScrollBottom.Save(activeConsole, messageArea);
    UpdateScrollback(1);
  }
  messageArea.appendChild(element);
  if (!noBufferMaintenance)
    ScrollBottom.Restore(activeConsole, messageArea);
}
function AddCSSRule(styleSheetIndex, ruleName) {
  if (styleSheetIndex < 0)
    styleSheetIndex = document.styleSheets.length + styleSheetIndex; 
  var stylesheet = document.styleSheets[styleSheetIndex];
  if (stylesheet.addRule)
    stylesheet.addRule(ruleName, null, 0); // for IE
  else 
    stylesheet.insertRule(ruleName + ' { }', 0); // other user agents
	if (stylesheet.rules)
		return stylesheet.rules[0]; // for IE
  else
		return stylesheet.cssRules[0]; // other user agents
}
function DebugMode(enabled) {
  if (!enabled) {
    // Disable tracing
    var tracing = document.getElementsByName(NAME.TRACING);
    tracing[0].checked = false;
    for (var i = 0; i < tracing.length; ++i)
      tracing[i].parentNode.style.display = 'none';
  }
}
function CreateEventHandler(action) {
  return function(event) {
    if (!event)
      var event = window.event;
    if (event.srcElement)
      event.target = event.srcElement;
    action(event);
  }
}
function AmendCheckboxOptionEvent(checkbox) {
  return CreateEventHandler(function(e) {
      if (e.target != checkbox) {
        e.target.firstChild.checked = !e.target.firstChild.checked;
        checkbox.onclick();
      }        
    }
  );
}
function GetChildElementsByTagName(parent, lowerCaseTagName) {
  var elements = new Array();
  for (var i = 0; i < parent.childNodes.length; ++i) {
    if (parent.childNodes[i].nodeName.toLowerCase() == lowerCaseTagName)
      elements.push(parent.childNodes[i]);
  }
  return elements;
}

// Utility objects and classes
var ScrollBottom = new Object();
ScrollBottom.Save = function(console, obj) {
  // Calculate current offset of scrollbar from bottom
  console.scrollBottom =  obj.scrollHeight - (obj.scrollTop + obj.clientHeight);
};
ScrollBottom.Restore = function(console, obj) {
  // Scroll to last line but only if scrollbar was already at the bottom
  if (console.scrollBottom == 0)
    obj.scrollTop = obj.scrollHeight;
};
function Palette(colors) {
  this.colors = colors;
  this.GetForString = function(name) {
    var val = 0;
    for (var i = 0; i < name.length; ++i)
      val ^= name.charCodeAt(i);
    return this.colors[val % this.colors.length];
  }
};
