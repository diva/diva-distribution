// Global constants and variables
var OPEN = '+';
var CLOSE = '\u2212'; // Minus sign
var INDENTSIZE = 4; // No. of whitespace characters per indentation level
var folderIndicators;
// Initialization
function DoOnload() {
  document.getElementById('loading').style.display = 'inline';
  var rootFolder = GetChildItems(document.getElementById('inventory'))[0];
  for (var i = 0; i < rootFolder.childNodes.length; ++i) {
    if (rootFolder.childNodes[i].nodeType == 1) { // Element nodes only
      // Hide root folder entry
      if (rootFolder.childNodes[i].nodeName != 'DIV')
        rootFolder.childNodes[i].style.display = 'none';
      // Retrieve folder indicator from root folder name, and set up regex for item names
      if (rootFolder.childNodes[i].nodeName == 'SPAN') {
        var rootFolderName = rootFolder.childNodes[i].getElementsByTagName('span')[1].firstChild.nodeValue;
        folderIndicators = new RegExp('[' + rootFolderName.charAt(0) + OPEN + CLOSE + ']');
      }
    }
  }
  // Expand each top level folder but collapse all of their subfolders
  var topLevelFolders = GetChildItems(rootFolder);
  for (var i = 0; i < topLevelFolders.length; ++i) {
    AdjustIndent(topLevelFolders[i]);
    Expand(topLevelFolders[i]);
  }
  document.getElementById('loading').style.display = 'none';
}
// Functions called from form
function Collapse(parent) {
  ApplyToItems(parent, OPEN, 'Expand(this.parentNode)',
    function(childItem) {
      childItem.style.display = 'none';
    }
  );
}
function Expand(parent) {
  ApplyToItems(parent, CLOSE, 'Collapse(this.parentNode)',
    function(childItem) {
      childItem.style.display = 'block';
      if (State.GetFor(childItem) == null) {
        AdjustIndent(childItem);
        Collapse(childItem);
        State.SetFor(childItem, State.VISITED);
      }
    }
  );
}
// Instrumentation of inventory items
function ApplyToItems(parent, indicator, onclickAction, childModifyFn) {
  var children = GetChildItems(parent);
  if (children.length > 0) {
    // Set click event handler
    var item = parent.getElementsByTagName('span')[0];
    item.onclick = new Function(onclickAction);
    item.style.cursor = 'pointer';
    // Set open/close indicator
    var indentation = item.getElementsByTagName('span')[1].firstChild;
    indentation.nodeValue = indentation.nodeValue.replace(folderIndicators, indicator);
    // Modify child items
    for (var i in children)
      childModifyFn(children[i]);
  }
}
function AdjustIndent(parent) {
  var indentation = parent.getElementsByTagName('span')[2].firstChild;
  indentation.nodeValue = indentation.nodeValue.substr(INDENTSIZE - 1);
  State.SetFor(parent, State.INDENT);
}
// Item state helper object
var State = {
  INDENT:'indent',
  VISITED:'visited',
  SetFor: function(parent, state) {
    parent.setAttribute('state', state);
  },
  GetFor: function(parent) {
    return parent.getAttribute('state');
  }
}
// Auxiliary functions
function GetChildItems(parent) {
  var elements = new Array();
  for (var i = 0; i < parent.childNodes.length; ++i) {
    if (parent.childNodes[i].nodeName == 'DIV')
      elements.push(parent.childNodes[i]);
  }
  return elements;
}
