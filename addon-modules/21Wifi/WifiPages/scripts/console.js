// Constants & Singletons
var TRACE = "trace"; // Trace messages CSS class
var MSG = "console"; // Console messages CSS class
var CSSINDEX = -1; // the last, i.e. the embedded style sheet
var ID = {
  CFGINFO:'configinfo',
  CFGERROR:'configerror',
  OPTIONS:'options',
  LOGIN:'login',
  SIMULATORS:'simulators',
  MESSAGES:'messages',
  TABULATORS:'tabulators',
  DEFAULT_CONSOLE:'rootconsole',
  PROMPT:'prompt',
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
var LinePattern = /(\d{2}:\d{2}:\d{2}\s*-\s*\[)([\w\s!]*)(\]:*\s*)(.*)/;
var loginAreaRule;
var outputAreaRule;
// Global variables
var wifi = false;
var element = new Object(); // References to frequently used elements
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
  // Set up element references
  element.TABULATORS = document.getElementById(ID.TABULATORS);
  element.SETTINGS = document.forms[NAME.SETTINGS];
  element.OPTIONS = document.getElementById(ID.OPTIONS);
  element.CREDENTIALS = document.forms[NAME.CREDENTIALS];
  element.DEFAULT_CONSOLE = document.getElementById(ID.DEFAULT_CONSOLE);
  element.MESSAGES = document.getElementById(ID.MESSAGES);
  element.COMMANDLINE = document.forms[ID.COMMAND][ID.COMMAND];
  element.PROMPT = document.getElementById(ID.PROMPT);
  // Define the default console that connects to the Wifi server
  activeConsole = new Console(ID.DEFAULT_CONSOLE);
  activeConsole.rule = AddCSSRule(CSSINDEX, '#console #messages .' + ID.DEFAULT_CONSOLE);
  activeConsole.rule.style.display = 'block';
  consoles[activeConsole.name] = activeConsole;
  consoles.count = 1;
  element.DEFAULT_CONSOLE.getElementsByTagName('span')[0].style.display = 'none';
  element.OPTIONS.getElementsByTagName('li')[1].style.display = 'none';
  document.getElementById(ID.LOGIN).getElementsByTagName('div')[0].style.display = 'none';
  //document.getElementById(ID.SIMULATORS).style.display = 'none';
  // Amend event handlers of some menu options
  var options = element.OPTIONS.getElementsByTagName('li');
  options[2].onclick = AmendCheckboxOptionEvent(document.getElementsByName(NAME.MONOSPACE)[0]);
  options[3].onclick = AmendCheckboxOptionEvent(document.getElementsByName(NAME.TRACING)[0]);
  options[4].onclick = AmendCheckboxOptionEvent(document.getElementsByName(NAME.TRACING)[1]);
  options[5].onclick = CreateEventHandler(function(e) {
      if (e.target != document.getElementsByName(NAME.BUFSIZE)[0])
        e.target.firstChild.select();
    }
  );
  // Initialize other globals
  NO_CONNECTION = document.getElementsByName(NAME.TEXT)[0].value;
  DebugMode(element.SETTINGS[NAME.DEBUG].value);
  // Get default parameters from form
  ChangedOption(NAME.TRACING);
  ChangedOption(NAME.MONOSPACE);
  // With Wifi, we have all data ready and can connect immediately
  element.CREDENTIALS.address.value = location.host;
  if (element.CREDENTIALS.user.value && element.CREDENTIALS.password.value) {
    wifi = {
      HEARTBEAT_THRESHOLD:60, // No. of ReadResponses requests before a heartbeat is sent
      heartbeatCounter:0
    };
    var buttons = document.getElementById(ID.COMMAND).getElementsByTagName('input');
    buttons[buttons.length-1].style.display = 'none';
    buttons[buttons.length-2].style.display = 'none';
    simulatorNode = document.getElementById(ID.SIMULATORS).cloneNode(true);
    Connect();
  }
  else
    loginAreaRule.style.display = 'block';
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
  this.title = null;
  this.prompt = null;
  this.consoleRequest = null;
  this.sessionId = null;
  this.helpNode = null;
  this.rule = null; // CSS rule
}

function Connect() {
  var address = element.CREDENTIALS.address.value;
  if (!address) {
    if (simulators.length > 1) {
      for (var i = 0; i < element.CREDENTIALS.simulator.length; ++i)
        if (element.CREDENTIALS.simulator[i].checked) {
          address = element.CREDENTIALS.simulator[i].value;
          break;
        }
    }
    else
      address = element.CREDENTIALS.simulator.value;
  }
  activeConsole.serviceURL = "http://" + address;
  activeConsole.user = element.CREDENTIALS.user.value;
  activeConsole.password = element.CREDENTIALS.password.value;
  StartSession(activeConsole);
}
function CreateConsole() {
  // New console
  var name = 'console' + consoles.count;
  var console = new Console(name);
  consoles[console.name] = console;
  consoles.count++;
  // New CSS rule with name as class
  console.rule = AddCSSRule(CSSINDEX, '#console #messages .' + name);
  console.rule.style.display = 'none'; // hide messages for now
  // New tab (clone from default)
  var node = element.DEFAULT_CONSOLE.cloneNode(true);
  node.setAttribute('id', name);
  var children = node.getElementsByTagName('span');
  //children[0].style.cursor = 'pointer';
  children[0].style.display = 'none';
  
  children[1].firstChild.data = "New\xA0Console";
  children[2].setAttribute('class', 'normal');
  element.TABULATORS.appendChild(node);
  // Make new console the current one
  ToggleTab(activeConsole, false);
  ToggleTab(console, true);
  ToggleConsole(false); // show login area
  activeConsole.rule.style.display = 'none'; // hide messages of previously active console
  activeConsole = console;
  // With Wifi, select from current list of simulators
  if (wifi)
    SelectSimulator();
}
function Disconnect() {
  var console = activeConsole;
  if (console.name != ID.DEFAULT_CONSOLE)
    SwitchConsole(ID.DEFAULT_CONSOLE);
  CloseSession(console);
  if (console.name != ID.DEFAULT_CONSOLE) {
    // Remove tab
    var tab = document.getElementById(console.name);
    element.TABULATORS.removeChild(tab);
  }
}
function SwitchConsole(name) {
  var console = consoles[name];
  ToggleTab(activeConsole, false);
  ToggleTab(console, true);
  activeConsole.rule.style.display = 'none'; // hide messages of formerly active console
  activeConsole = console;
  if (activeConsole.sessionId) // connected?
    ToggleConsole(true); // show messages of active console
  else
    ToggleConsole(false);
}
function ToggleTab(console, active) {
  var tab = document.getElementById(console.name);
  var styleClass = tab.getAttribute('class');
  if (active) {
    styleClass = styleClass.replace(/\s*inactive/, '');
    tab.onclick = null;
    ShowPrompt(console);
    if (console.name != ID.DEFAULT_CONSOLE) {
      element.OPTIONS.getElementsByTagName('li')[1].style.display = 'block';
      /*
      // Show close button
      tab.getElementsByTagName('span')[0].style.display = 'inline';
      tab.getElementsByTagName('span')[0].onclick = Disconnect;
      //tab.getElementsByTagName('span')[0].onclick = new Function('Disconnect("' + console.name + '")');
      */
    }
  }
  else {
    ScrollBottom.Save(console, element.MESSAGES);
    styleClass += ' inactive';
    /*
    tab.getElementsByTagName('span')[0].style.display = 'none'; // hide close button
    tab.getElementsByTagName('span')[0].onclick = null;
    */
    tab.onclick = new Function('SwitchConsole("' + console.name + '")');
    HidePrompt(console);
    if (console.name != ID.DEFAULT_CONSOLE) {
      element.OPTIONS.getElementsByTagName('li')[1].style.display = 'none';
    }
  }
  tab.setAttribute('class', styleClass);
}
function ToggleConsole(visible) {
  loginAreaRule.style.display = (visible ? 'none' : 'block');
  outputAreaRule.style.display = (visible ? 'block' : 'none');
  element.MESSAGES.style.display = (visible ? 'block' : 'none');
  if (visible) {
    activeConsole.rule.style.display = 'block';
    ScrollBottom.Restore(activeConsole, element.MESSAGES);
  }
}

// Create selection form with available simulators
function SelectSimulator() {
  var query = location.protocol.concat('//', location.host, location.pathname, 'data/simulators/', location.search);
  Output(TRACE, "[SelectSimulator] Requesting available simulators: " + query);
  var regionRequest = AjaxSend(query, null,
    function(xml) {
      var allSimulators = xml.getElementsByTagName('Simulator');
      Output(TRACE, "[SelectSimulator] Found ".concat(allSimulators.length, " simulator(s) with a total of ", xml.getElementsByTagName('Region').length, " region(s)"));
      simulators = new Array();
      for (var i = allSimulators.length - 1; i >= 0; --i) {
        if (allSimulators[i].getAttribute('HostAddress') != consoles[ID.DEFAULT_CONSOLE].serviceURL.substr(7))
          simulators.push(allSimulators[i]);
      }
      if (simulators.length > 0) {
        var loginForm = document.getElementsByName(NAME.CREDENTIALS)[0];
        // Hide unused form elements
        var inputs = loginForm.getElementsByTagName('p');
        for (var i = 1; i <= 3; ++i)
          inputs[inputs.length-i].style.display = 'none';
        // Change form to offer available simulators
        var chooser = loginForm.getElementsByTagName('div')[0];
        loginForm.replaceChild(document.createElement('div'), chooser);
        chooser = loginForm.getElementsByTagName('div')[0];
        chooser.setAttribute('id', ID.SIMULATORS);
        //chooser.appendChild(document.createTextNode("Please select a simulator:"));
        chooser.appendChild(simulatorNode.getElementsByTagName('span')[0].cloneNode(true));
        for (var i = 0; i < simulators.length; ++i) {
          // Retrieve simulator info
          var address = simulators[i].getAttribute('HostAddress');
          var regionElements = simulators[i].getElementsByTagName('Name');
          var regions = new Array();
          for (var j = 0; j < regionElements.length; ++j)
            regions.push(regionElements[j].firstChild.data);
          regions.sort();
          // Add radio button for simulator
          var option = simulatorNode.getElementsByTagName('p')[0].cloneNode(true);
          GetChildElementsByTagName(option, 'input')[0].value = address;
          if (0 == i) 
            GetChildElementsByTagName(option, 'input')[0].checked = true;
          GetChildElementsByTagName(option, 'span')[0].innerHTML = address;
          option.appendChild(document.createTextNode(" " + regions.join(", ") + "."));
          chooser.appendChild(option);
          // Prepare credentials
          element.CREDENTIALS.address.value = '';
          element.CREDENTIALS.user.value = consoles[ID.DEFAULT_CONSOLE].user;
          element.CREDENTIALS.password.value = consoles[ID.DEFAULT_CONSOLE].password;
         }
        chooser.style.display = 'block';
      }
      else {
        Disconnect();
        alert(element.SETTINGS[NAME.TEXT][1].value);
      }
    }
  );
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
          if (elements && elements.length > 0 && elements[0].nodeType == 1) {
            // Remove no longer needed info elements
            var e = document.getElementById(ID.CFGINFO);
            if (e) e.parentNode.removeChild(e);
            e = document.getElementById(ID.CFGERROR);
            if (e) e.parentNode.removeChild(e);
            // Parse response
            console.sessionId = elements[0].firstChild.nodeValue;
            SetTitle(console, xml.getElementsByTagName('Prompt')[0].firstChild.nodeValue);
            console.helpNode = xml.getElementsByTagName('HelpTree')[0];
            Output(TRACE, "[StartSession:".concat(console.name, "] SessionId=" + console.sessionId + " Prompt='" + console.title + "' Help entries:" + console.helpNode.childNodes.length));
            // Get console output
            void AjaxSend(console.serviceURL.concat('/ReadResponses/', console.sessionId, '/'), '',
              function(xml, status) { ReadResponses(console, xml, status); },
              console.consoleRequest);
          }
          else {
            // Show error message
            document.getElementById(ID.CFGERROR).style.display = 'block';
          }
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
    /*
    if (console.consoleRequest.xhr)
      console.consoleRequest.xhr.abort();
    */
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
      ScrollBottom.Save(console, element.MESSAGES);
      UpdateScrollback(elements.length);
      for (var i = 0; i < elements.length; ++i) {
        var lineNode = elements[i];
        lineNode.removeAttribute('Number');
        //var lines = lineNode.firstChild.nodeValue.split('\n');
        //var parts = lines.shift().split(':');
        var parts = lineNode.firstChild.nodeValue.split(':');
        parts.shift(); // discard line number
        var level = parts.shift();
        var line = parts.join(':');
        if (line.substr(1, 2) == '++') { // Prompt
          if (line.substr(0, 1) == '+') {
            // Normal prompt is used as title
            SetTitle(console, line.substr(3));
          }
          else {
            // Interactive prompt for requesting input
            ShowPrompt(console, line.substr(3));
          }
          UpdateScrollback(1); Output(TRACE, "[ReadResponses:".concat(console.name, "] '", line, "'"), true);
          continue;
        }
        else if (line.length >= 1024) {
          UpdateScrollback(1); Output(TRACE, "[ReadResponses:".concat(console.name, "] Long line with ", line.length, " bytes"), true);
        }
        if (console.prompt)
          HidePrompt(console, true);
        // Format line
        var matches = line.match(LinePattern);
        //line = new Array();
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
        //if (lines.length)
        //  UpdateScrollback(lines.length);
        //for (var l in lines)
        //  line.push(document.createTextNode(lines[l]));
        Output(MSG + " " + console.name, line, true);
      }
      ScrollBottom.Restore(console, element.MESSAGES);
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
  if (wifi && (console.name == ID.DEFAULT_CONSOLE))
    if (++wifi.heartbeatCounter >= wifi.HEARTBEAT_THRESHOLD) {
      // Keep session active
      wifi.heartbeatCounter = 0;
      Output(TRACE, "[ReadResponses:".concat(console.name, "] Wifi Heartbeat"));
      void AjaxSend(location.protocol.concat('//', location.host, location.pathname, 'heartbeat/', location.search));
    }
}
function Command(console, command) {
  command = command.replace(/\s*$/, '');
  if (console.sessionId) {
    Output(TRACE, "[Command:".concat(console.name, "] Sending command '" + command + "'"));
    var loggedPrompt = console.title;
    if (console.prompt)
      loggedPrompt = console.prompt;
    Output(MSG + " " + console.name, loggedPrompt + " " + command);
    try {
      void AjaxSend(console.serviceURL.concat('/SessionCommand/'), 'ID=' + console.sessionId + '&COMMAND=' + command,
        function(xml, status) {
          ShowStatus(console, status);
          if (xml) {
            // Process SessionCommand response
            var elements = xml.getElementsByTagName('Result');
            Output(TRACE, "[Command:".concat(console.name, "] Result=" + elements[0].firstChild.nodeValue));
            // Clear command line
            element.COMMANDLINE.value = "";
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
function SetTitle(console, newPrompt) {
  console.title = newPrompt.replace(/\s*#?\s*$/, " #");
  document.getElementById(console.name).getElementsByTagName('span')[1].firstChild.data = console.title.slice(0, -2).replace(/ /g, '\xA0');
}

function ShowPrompt(console, newPrompt) {
  if (newPrompt)
    console.prompt = newPrompt.replace(/\s*$/, "");;
  if (console.prompt) {
    element.PROMPT.firstChild.data = console.prompt;
    element.PROMPT.style.display = 'block';
  }
}

function HidePrompt(console, clear) {
  if (clear)
    console.prompt = null;
  element.PROMPT.style.display = 'none';
}

// Other Form actions
function ChangedOption(name) {
  if (NAME.MONOSPACE == name) {
    // Use monospace or proportional font
    if (element.SETTINGS[name].checked)
      element.MESSAGES.setAttribute('class', 'monospace');
    else
      element.MESSAGES.removeAttribute('class');
  }
  else if (NAME.TRACING == name) {
    // Hide or show trace message elements
    ScrollBottom.Save(activeConsole, element.MESSAGES);
    logMessageRule.style.display = (element.SETTINGS[name][1].checked ? 'none' : 'block');
    ScrollBottom.Restore(activeConsole, element.MESSAGES);
  }
  else if (NAME.BUFSIZE == name)
    UpdateScrollback();
}
function UpdateScrollback(addLines) {
  var excessLines = element.MESSAGES.childNodes.length + (addLines ? addLines : 0) - element.SETTINGS.bufsize.value;
  while (excessLines-- > 0 && element.MESSAGES.childNodes.length)
    element.MESSAGES.removeChild(element.MESSAGES.firstChild);
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
    //function() { return new XDomainRequest(); },
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
  var hasXDR = false; //(typeof(XDomainRequest) != "undefined");
  if (request && !hasXDR)
    request.callback = callback;
  else
    request = new AjaxRequest(callback);
  Output(TRACE, "[AJAX:".concat(request.id, "] ", url, " data='", postData, "'"));
  var method = (typeof(postData) == 'string') ? 'POST' : 'GET';
  var async = (request.callback) ? true : false;
  // Set up the request
  try {
    if (hasXDR) {
      request.xhr.open(method, url);
      request.xhr.onerror = function() {
        Output(TRACE, "[AJAX:".concat(request.id, "] XDR error"));
      };
      request.xhr.onload = function() { AjaxReceiveXDR(request); };
    }
    else {
      request.xhr.open(method, url, async);
      //request.xhr.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded'); // This forces a preflight request
      if (async)
          request.xhr.onreadystatechange = function() { AjaxReceive(request); };
    }
    request.xhr.send(postData);
    if (!async && !hasXDR)
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
function AjaxReceiveXDR(request) {
  // Parse XDR response and prepare request for generic AjaxReceive
  Output(TRACE, "[AJAX:".concat(request.id, "] Parse XDR response"));
  var xmlDoc = new ActiveXObject('Microsoft.XMLDOM');
  xmlDoc.async = 'false';
  xmlDoc.loadXML(request.xhr.responseText);
  request.xhr.responseXML = xmlDoc;
  request.xhr.readyState = 4;
  request.xhr.status = 200;
  request.xhr.statusText = "OK";
  AjaxReceive(request);
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
  if (request.callback)
    request.callback(request.xhr.responseXML, new XMLHttpStatus(request.xhr.status, statusText));
}

// Auxiliary functions
function Output(cssClass, logObj, noBufferMaintenance) {
  if (cssClass == TRACE && !element.SETTINGS.trace[0].checked)
    return;
  var elem = document.createElement('div');
  elem.setAttribute('class', cssClass);
  if (typeof(logObj) == 'string')
    elem.appendChild(document.createTextNode(logObj));
  else { // assume an array with nodes
    for (var i = 0; i < logObj.length; ++i)
      elem.appendChild(logObj[i]);
  }
  if (!noBufferMaintenance) {
    ScrollBottom.Save(activeConsole, element.MESSAGES);
    UpdateScrollback(1);
  }
  element.MESSAGES.appendChild(elem);
  if (!noBufferMaintenance)
    ScrollBottom.Restore(activeConsole, element.MESSAGES);
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
