function showForm(divName, form) {
  $('#' + divName).html(form);
}

function checkExtension(req_ext) {
  var ext = document.upload.datafile.value;
  ext = ext.substring(ext.length-3,ext.length);
  ext = ext.toLowerCase();
  if(ext != req_ext.toLowerCase()) {
    alert('Please select a .' + req_ext+ ' file!');
    return false; 
  }
  else
    return true; 
}

