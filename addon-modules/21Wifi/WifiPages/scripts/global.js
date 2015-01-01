// Delegate Load and Unload events to scripts which are not defined in HTML head
function OnLoad() {
  try {
    DoOnload();
  }
  catch (e) { }
}
function OnUnload() {
  try {
    DoOnunload();
  }
  catch (e) { }
}
window.onload = OnLoad;
window.onunload = OnUnload;
