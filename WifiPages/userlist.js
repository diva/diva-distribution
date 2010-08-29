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
    document.write(LocaleDateString(rfc1123Date));
} 