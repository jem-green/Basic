using System;
using System.Web.UI;
using uBasicLibrary;
using log4net;
using System.IO;

namespace uBasicWeb
{
    public partial class SuperStarTrek : System.Web.UI.Page, System.Web.UI.ICallbackEventHandler
    {
        static readonly IConsoleIO textAreaIO = new TextAreaIO();
        protected String returnValue;
        IInterpreter basic;

        protected void Page_Load(object sender, EventArgs e)
        {

            // this is where i would want to create a new session object which is the ubasic engine.

            string cbReference = Page.ClientScript.GetCallbackEventReference(this, "arg", "ReceiveServerData", "context");
            string callbackScript;
            callbackScript = "function CallServer(arg, context)" + "{ " + cbReference + ";}";
            Page.ClientScript.RegisterClientScriptBlock(this.GetType(), "CallServer", callbackScript, true);

            if (!Page.IsPostBack)
            {

                char[] program;
                string input = Server.MapPath("~/SuperStarTrek.txt");

                using (StreamReader sr = new StreamReader(input))
                {
                    program = sr.ReadToEnd().ToCharArray();
                }

                basic = new Altair.Interpreter(program, textAreaIO);
                basic.Init(0);

                System.Threading.Thread thread = new System.Threading.Thread(new System.Threading.ThreadStart(Run));
                thread.Start();
            }
        }

        protected void Run()
        {
            do
            {
                basic.Run();
            } while (!basic.Finished());
        }

        public void RaiseCallbackEvent(String eventArguments)
        {
            // Pass the string to the TextAreIO
            if (eventArguments != "")
            {
                textAreaIO.Input = eventArguments;
            }
            // Need to echo back the input
            returnValue = textAreaIO.Output;
        }

        public string GetCallbackResult()
        {
            return (returnValue);
        }
    }
}