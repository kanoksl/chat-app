using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatClient
{
    public static class ClientProgram
    {
        /// <summary>
        /// Start the client application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MultiFormContext(
                new ChatWindow(defaultUsername: "dog"),
                new ChatWindow(defaultUsername: "cat"),
                new ChatWindow(defaultUsername: "pig"),
                new ChatWindow(defaultUsername: "cow")
            ));
        }
    }

    public class MultiFormContext : ApplicationContext
    {
        private int formCount;

        public MultiFormContext(params Form[] forms)
        {
            formCount = forms.Length;
            foreach (var form in forms)
            {
                form.FormClosed += (sender, args) =>
                {
                    if (Interlocked.Decrement(ref formCount) == 0)
                        ExitThread();
                };
                form.Show();
            }
        }
    }
}
