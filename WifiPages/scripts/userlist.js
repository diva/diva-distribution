function DoOnload() {
  if (document.getElementById('pending'))
    InitPendingTableRowSort();
  if (document.getElementById('users'))
    InitUserTableRowSort();
}
// Configuration for sorting rows in pending users table
var pending; // variable name must match the table id
function InitPendingTableRowSort() {
  pending = new Array();
  pending[0] = { column:0, reverse:false, comparer:function(a, b) {return CompareTextCells(a, b, 0);} }; // Name
  pending[1] = { column:2, reverse:false, comparer:function(a, b) {return CompareDateCells(a, b, 2);} }; // Date
  SetupTableHeadings('pending', pending);
}
// Configuration for sorting rows in users table
var users; // variable name must match the table id
function InitUserTableRowSort() {
  users = new Array();
  users[0] = { column:0, reverse:false, comparer:function(a, b) {return CompareTextCells(a, b, 0);} }; // Name
  users[1] = { column:2, reverse:false, comparer:function(a, b) {return CompareTextCells(a, b, 2);} }; // Title
  users[2] = { column:3, reverse:false, comparer:function(a, b) {return CompareNumCells(a, b, 3);} };  // Level
  users[3] = { column:4, reverse:false, comparer:function(a, b) {return CompareDateCells(a, b, 4);} }; // Created
  SetupTableHeadings('users', users);
}
// Add event handlers for row sorting
function SetupTableHeadings(tableId, columnConfig) {
  var table = document.getElementById(tableId);
  if (table.rows == null || table.rows.length <= 2)
    return;
  for (var i = 0; i < columnConfig.length; ++i) {
    var heading = table.rows[0].cells[columnConfig[i].column];
    heading.onclick = new Function("SortColumn('" + tableId + "', " + i +")");
    heading.style.cursor = "pointer";
    heading.title = "Sort rows";
  }
}
// Sort user table rows by values in a column
function SortColumn(id, index) {
  var columnConfig = eval(id);
  var table = document.getElementById(id);
  var tableContent = table.getElementsByTagName('tbody')[0];
  var rows = tableContent.getElementsByTagName('tr');
  var rowcount = rows.length;
  // Copy rows into array
  var sortArray = new Array(rowcount);
  for(var i = 0; i < rowcount; ++i)
    sortArray[i] = rows[i];
  // Do the sorting
  sortArray.sort(columnConfig[index].comparer);
  if (columnConfig[index].reverse)
    sortArray.reverse();
  columnConfig[index].reverse = !columnConfig[index].reverse;
  // Update table with sorted rows
  for(var i = 0; i < rowcount; ++i)
    tableContent.appendChild(sortArray[i]);

  UpdateTableHeadings(table, columnConfig, index);
}
// Display sort direction indicators
function UpdateTableHeadings(table, columnConfig, index) {
  // Constants; IE does not like const declarator
  var normal = String.fromCharCode(0x25bc); // down arrow
  var reverse = String.fromCharCode(0x25b2); // up arrow
  var indicators = new RegExp('(' + normal + '|' + reverse + ')$');
  
  var cells = table.rows[0].cells;
  for (var i = 0; i < columnConfig.length; ++i) {
    var text = cells[columnConfig[i].column].firstChild.nodeValue;
    // Remove any indicator
    text = text.replace(indicators, '');
    // Add indicator for selected column
    if (i == index) {
      if (columnConfig[i].reverse)
        text += reverse;
      else
        text += normal;
    }
    cells[columnConfig[i].column].firstChild.nodeValue = text;
  }
}
// Comparison functions for array sort
function CompareNumCells(a, b, column) {
  return a.cells[column].firstChild.nodeValue - b.cells[column].firstChild.nodeValue;
}
function CompareTextCells(a, b, column) {
  var contentA = a.cells[column].firstChild.nodeValue;
  var contentB = b.cells[column].firstChild.nodeValue;
  return CompareText(contentA, contentB);
}
function CompareDateCells(a, b, column) {
  var contentA = a.cells[column].getElementsByTagName('script')[0].nextSibling.nodeValue;
  var contentB = b.cells[column].getElementsByTagName('script')[0].nextSibling.nodeValue;
  return CompareText(contentA, contentB);
}
function CompareText(a, b) {
  if (a > b) return 1;
  if (a < b) return -1;
  return 0;
}

// Format date & time with user's locale
function LocaleDateString(d) {
    return d.toLocaleString();
}
// Alternative: Format date & time similar to ISO 8601, without seconds
function IsoLikeDateString(d) {
    function pad(n) { return n < 10 ? '0' + n : n; }
    return d.getFullYear() + '-'
      + pad(d.getMonth() + 1) + '-'
      + pad(d.getDate()) + ' '
      + pad(d.getHours()) + ':'
      + pad(d.getMinutes());
}
// Convert date & time from RFC 1123 format
function ConvertAndWriteDate(rfc1123DateString) {
    if (rfc1123DateString == '')
        return;
    var rfc1123Date = new Date(rfc1123DateString);
    document.write(IsoLikeDateString(rfc1123Date));
}
