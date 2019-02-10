using System.IO;
using System.Text;
using Akka.Actor;
using WinTail.Utilities;

namespace WinTail.Actors
{
    public class TailActor : UntypedActor
    {
        #region Message Types

        /// <summary>
        /// Signal that the file has changed, and we need to read the next line.
        /// </summary>
        public class FileWrite
        {
            public string FileName { get; }
            public FileWrite(string fileName)
            {
                FileName = fileName;
            }
        }

        /// <summary>
        ///  Signal that the OS had an error accessing the file.
        /// </summary>
        public class FileError
        {
            public string FileName { get; }
            public string Reason { get; }

            public FileError(string fileName, string reason)
            {
                FileName = fileName;
                Reason = reason;
            }
        }

        /// <summary>
        /// Signal to read initial contents of file at actor startup
        /// </summary>
        public class InitialRead
        {
            public string FileName { get; }
            public string Text { get; }

            public InitialRead(string fileName, string text)
            {
                FileName = fileName;
                Text = text;
            }
        }
        #endregion

        private readonly string _filePath;
        private readonly IActorRef _reporterActor;
        private FileObserver _observer;
        private Stream _fileStream;
        private StreamReader _fileStreamReader;

        public TailActor(IActorRef reporterActor, string filePath)
        {
            _reporterActor = reporterActor;
            _filePath = filePath;
        }

        protected override void PreStart()
        {
            // start watching file for changes
            _observer = new FileObserver(Self, Path.GetFullPath(_filePath));
            _observer.Start();
            
            // open file stream while allowing file to be written to
            _fileStream = new FileStream(Path.GetFullPath(_filePath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _fileStreamReader = new StreamReader(_fileStream, Encoding.UTF8);
            
            // read initial contents and send to console
            var text = _fileStreamReader.ReadToEnd();
            Self.Tell(new InitialRead(_filePath, text));
        }

        protected override void OnReceive(object message)
        {
            if (message is FileWrite)
            {
                // pull results from cursor to eof and write to output
                var text = _fileStreamReader.ReadToEnd();
                if (!string.IsNullOrEmpty(text))
                    _reporterActor.Tell(text);
            }
            else if (message is FileError)
            {
                var fe = message as FileError;
                _reporterActor.Tell($"Tail error: { fe.Reason }");
            }
            else if (message is InitialRead)
            {
                var ir = message as InitialRead;
                _reporterActor.Tell(ir.Text);
            }
        }

        /// <summary>
        /// Make sure to clean up after ourselves.
        /// </summary>
        protected override void PostStop()
        {
            _observer.Dispose();
            _observer = null;
            _fileStreamReader.Close();
            _fileStreamReader.Dispose();
            base.PostStop();
        }
    }
}