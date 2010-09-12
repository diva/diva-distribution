// Global constants and variables
var OPEN = '+';
var CLOSE = '\u2212'; // Minus sign
var INDENTSIZE = 4; // No. of whitespace characters per indentation level
var itemNamePattern;
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
        var rootFolderName = rootFolder.childNodes[i].getElementsByTagName('span')[0].firstChild.nodeValue;
        itemNamePattern = new RegExp(
          "^([" + State.INDENT + State.VISITED + "]?\\xA0*)" +   // Indentation whitespace (w/ state info)
          "[" + rootFolderName.charAt(0) + OPEN + CLOSE + "]?" + // Folder indicators
          "(\\s+.*)"                                             // Item name
        );
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
      if (State.GetFor(childItem) != State.VISITED) {
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
    var name = item.getElementsByTagName('span')[0].firstChild;
    var matches = name.nodeValue.match(itemNamePattern);
    name.nodeValue = matches[1] + indicator + matches[2];
    // Modify child items
    for (var i in children) {
      if (State.GetFor(children[i]) == State.START)
        AdjustIndent(children[i]);
      childModifyFn(children[i]);
    }
  }
}
function AdjustIndent(parent) {
  var name = parent.getElementsByTagName('span')[1].firstChild;
  name.nodeValue = name.nodeValue.substr(INDENTSIZE - 1);
  State.SetFor(parent, State.INDENT);
}
// Item state helper object
// State information is stored in the indentation whitespace that is part of an item's name
var State = {
  START:'\u00A0',   // (Non-breaking space)
  INDENT:'\u200E',  // (Left-to-right mark)
  VISITED:'\u200B', // (Zero width space)
  /* For debugging:
  INDENT:'\u2027',  // (Hyphenation point)
  VISITED:'\u205A', // (Two dot punctuation)
  */
  SetFor: function(parent, state) {
    var name = parent.getElementsByTagName('span')[1].firstChild;
    name.nodeValue = state + name.nodeValue.substr(1);
  },
  GetFor: function(parent) {
    var name = parent.getElementsByTagName('span')[1].firstChild;
    return name.nodeValue.charAt(0);
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
