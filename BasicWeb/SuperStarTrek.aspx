<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="SuperStarTrek.aspx.cs" Inherits="BasicWeb.SuperStarTrek" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
    <head runat="server">
        <title>uBasic - Super Star Trek</title>
        <link href="https://fonts.googleapis.com/css?family=VT323" rel="stylesheet"/>
        <link href="uBasic.css" rel="stylesheet" />

        <script type="text/javascript">

            var value;

            function Load() {
                setInterval("CallServer('', '');", 5 * 1000);
                CallServer('', '');
                value = '';
            }

            function SendKey(sValue) {
                CallServer(sValue, "");
            }

            function ReceiveServerData(rValue) {
                if (rValue != '') {
                    document.getElementById("A").value = document.getElementById("A").value + rValue;
                    document.getElementById("A").scrollTop = document.getElementById("A").scrollHeight;
                }
            }

            function CaptureKey(event) {

                if (event.which == null) {
                    char = String.fromCharCode(event.keyCode);    // old IE
                }
                else if (event.which != 0 && event.charCode != 0) {
                    char = String.fromCharCode(event.which);	  // All others
                }
                else {
                    char = event.char;
                }

                if (char != '\n') {
                    var textarea = document.getElementById("A");
                    if (char == '\b') {
                        if (value.length > 0) {
                            value = value.substring(0, value.length - 1)
                            textarea.value = textarea.value.substring(0, textarea.value.length - 1)
                        }
                    }
                    else {
                        textarea.value = textarea.value + char;
                        value = value + char;
                    }
                }
                else
                {
                    value = value + char;
                    CallServer(value, '');
                    value = '';
                }
                return (false);
            }

        </script>
    </head>

    <body onload="Load();">
        <form id="form1" runat="server">
        <div>
            <textarea id="A" rows="32" cols="75" runat="server" onkeydown="return CaptureKey(event)"></textarea>
        </div>

        </form>
    </body>
</html>
