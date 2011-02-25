// Global constants and variables
var OPEN = '+';
var CLOSE = '\u2212'; // Minus sign
var INDENTSIZE = 4; // No. of whitespace characters per indentation level
var NAME = {
  INVENTORY:'inventory',
  SORT_ALPHA:'sort',
  SORT_TOPFOLDERS:'topfolders',
  ITEM:'item'
}
var element = new Object(); // References to frequently used elements
var folderIndicators;
var sortAlpha;
var sortTopFolders;
// Initialization
function DoOnload() {
  document.getElementById('loading').style.display = 'inline';
  element.SORT_ALPHA = document.forms[NAME.INVENTORY][NAME.SORT_ALPHA];
  element.SORT_TOPFOLDERS = document.forms[NAME.INVENTORY][NAME.SORT_TOPFOLDERS];
  element.ROOTFOLDER = document.getElementById('inventory').getElementsByTagName('div')[0];
  for (var i = 0; i < element.ROOTFOLDER.childNodes.length; ++i) {
    if (element.ROOTFOLDER.childNodes[i].nodeType == 1) { // Element nodes only
      // Hide root folder entry
      if (element.ROOTFOLDER.childNodes[i].nodeName != 'DIV')
        element.ROOTFOLDER.childNodes[i].style.display = 'none';
      // Retrieve folder indicator from root folder name, and set up regex for item names
      if (element.ROOTFOLDER.childNodes[i].nodeName == 'SPAN') {
        var rootFolderName = element.ROOTFOLDER.childNodes[i].getElementsByTagName('span')[1].firstChild.nodeValue;
        folderIndicators = new RegExp('[' + rootFolderName.charAt(0) + OPEN + CLOSE + ']');
      }
    }
  }
  sortAlpha = element.SORT_ALPHA.checked;
  sortTopFolders = element.SORT_TOPFOLDERS.checked;
  // Expand each top level folder but collapse all of their subfolders
  var topLevelFolders = GetChildItems(element.ROOTFOLDER);
  for (var i = 0; i < topLevelFolders.length; ++i) {
    AdjustEntry(topLevelFolders[i], i);
    Expand(topLevelFolders[i]);
  }
  document.getElementById('loading').style.display = 'none';
  document.getElementById('settings').style.display = 'inline';
}
// Functions called from form
function Collapse(parent) {
  ApplyToEntry(parent, OPEN, 'Expand(this.parentNode)',
    function(children) {
      for (var i in children)
        children[i].style.display = 'none';
    }
  );
}
function Expand(parent) {
  var sortingFn = null;
  if (sortAlpha && State.GetFor(parent) != State.SORTED)
    sortingFn = CompareEntryNames;
  if (!sortAlpha && State.GetFor(parent) == State.SORTED)
    sortingFn = CompareItemNumbers;
  ApplyToEntry(parent, CLOSE, 'Collapse(this.parentNode)',
    function(children) {
      for (var i in children)
        if (State.GetFor(children[i]) == null) {
          AdjustEntry(children[i], i);
          Collapse(children[i]);
          State.SetFor(children[i], State.VISITED);
        }
        
      if (sortingFn) // Sort child entries
        children.sort(sortingFn);
      for (var i in children) {
        var childItem = (sortingFn ? parent.appendChild(children[i]) : children[i]);
        // Show entry
        childItem.style.display = 'block';
        /*
        if (State.GetFor(childItem) == null) {
          AdjustEntry(childItem, i);
          Collapse(childItem);
          State.SetFor(childItem, State.VISITED);
        }
        */
      }
    }
  );
  State.SetFor(parent, (sortAlpha ? State.SORTED : State.VISITED)); 
}
function ChangedOption(name) {
  if (NAME.SORT_ALPHA == name) {
    sortAlpha = element.SORT_ALPHA.checked;
    if (sortAlpha) // Sort all visible entries
      SortVisibleFolder(element.ROOTFOLDER, CompareEntryNames, State.SORTED);
    else // Restore unsorted order for all visible entries
      SortVisibleFolder(element.ROOTFOLDER, CompareItemNumbers, State.VISITED);
  }
  else if (NAME.SORT_TOPFOLDERS == name) {
    sortTopFolders = element.SORT_TOPFOLDERS.checked;
    if (sortAlpha) // Sort folders to top with alphabetical order
      SortVisibleFolder(element.ROOTFOLDER, CompareEntryNames, null);
    else // Only sort folders to top
      SortVisibleFolder(element.ROOTFOLDER, CompareItemNumbers, null);
  }
}
// Instrumentation of inventory items
function SortVisibleFolder(element, sortingFn, resultingState) {
  if (element.getAttribute(NAME.ITEM) && (element.style.display == 'none' || State.GetFor(element) == resultingState))
    return;
  var children = GetChildItems(element);
  if (children.length > 0) {
    children.sort(sortingFn);
    for (var i in children) {
      var childItem = element.appendChild(children[i]);
      SortVisibleFolder(childItem, sortingFn, resultingState);
    }
  }
  if (resultingState)
    State.SetFor(element, resultingState);
}
function ApplyToEntry(parent, indicator, onclickAction, childModifyFn) {
  var children = GetChildItems(parent);
  if (children.length > 0) {
    // Set click event handler
    var item = parent.getElementsByTagName('span')[0];
    item.onclick = new Function(onclickAction);
    item.style.cursor = 'pointer';
    // Set open/close indicator
    var indentation = item.getElementsByTagName('span')[1].firstChild;
    indentation.nodeValue = indentation.nodeValue.replace(folderIndicators, indicator);
    // Invoke callback for modifying child items
    childModifyFn(children);
  }
}
function AdjustEntry(parent, number) {
  // Set sequential number
  parent.setAttribute(NAME.ITEM, number);
  // Adjust indentation
  var indentation = parent.getElementsByTagName('span')[2].firstChild;
  indentation.nodeValue = indentation.nodeValue.substr(INDENTSIZE - 1);
  State.SetFor(parent, State.ADJUSTED);
}
// Item state helper object
var State = {
  ADJUSTED:'adjusted',
  VISITED:'visited',
  SORTED:'sorted',
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
function CompareEntryTypes(a, b) {
  if (sortTopFolders) {
    var typeA = a.getElementsByTagName('input')[0].value;
    var typeB = b.getElementsByTagName('input')[0].value;
    if (typeA != typeB)
      return (typeA == 'folder') ? -1 : 1;
  }
  return 0;
}
function CompareEntryNames(a, b) {
  var typeComparison = CompareEntryTypes(a, b);
  if (typeComparison != 0)
    return typeComparison;
  a = a.getElementsByTagName('span')[3].firstChild.nodeValue
  b = b.getElementsByTagName('span')[3].firstChild.nodeValue
  if (a > b) return 1;
  if (a < b) return -1;
  return 0;
}
function CompareItemNumbers(a, b) {
  var typeComparison = CompareEntryTypes(a, b);
  if (typeComparison != 0)
    return typeComparison;
  return a.getAttribute(NAME.ITEM) - b.getAttribute(NAME.ITEM);
}
