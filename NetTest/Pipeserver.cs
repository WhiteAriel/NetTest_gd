using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;


namespace NetTest
{
    class PipeServer
    {
        static int numThreads = 1;

        static void OpenServer()
        {
            for (int i = 0; i < numThreads; i++)
            {
                Thread newThread = new Thread(ServerThread);
                newThread.Start();
            }
            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();
        } // Main()

        private static void ServerThread(object data)
        {
            NamedPipeServerStream pipeServer =
                new NamedPipeServerStream("robinTest", PipeDirection.InOut, numThreads);
            Console.WriteLine("NamedPipeServerStream thread created.");

            // Wait for a client to connect
            pipeServer.WaitForConnection();

            Console.WriteLine("Client connected.");
            try
            {
                // Read the request from the client. Once the client has
                // written to the pipe its security token will be available.
                StreamReader sr = new StreamReader(pipeServer);
                StreamWriter sw = new StreamWriter(pipeServer);
                sw.AutoFlush = true;

                // Verify our identity to the connected client using a
                // string that the client anticipates.
                sw.WriteLine("I am the one true server!");

                // Obtain the filename from the connected client.
                string filename = sr.ReadLine();

                // Read in the contents of the file while impersonating the client.
                ReadFileToStream fileReader = new ReadFileToStream(pipeServer, filename);

                // Display the name of the user we are impersonating.
                Console.WriteLine("Reading file: {0} as user {1}.",
                    pipeServer.GetImpersonationUserName(), filename);

                pipeServer.RunAsClient(fileReader.Start);

                pipeServer.Disconnect();
            }
            // Catch the IOException that is raised if the pipe is broken
            // or disconnected.
            catch (IOException e)
            {
                Console.WriteLine("ERROR: {0}", e.Message);
            }
            pipeServer.Close();
        } // ServerThread()

    }
}
