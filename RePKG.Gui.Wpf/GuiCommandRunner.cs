using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using RePKG.Command;

namespace RePKG.Gui.Wpf
{
    internal sealed class GuiCommandRunner
    {
        private readonly Action<string> _logger;

        public GuiCommandRunner(Action<string> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task RunExtractAsync(ExtractOptions options)
        {
            return RunWithConsoleCaptureAsync(() => Extract.Action(options));
        }

        public Task RunInfoAsync(InfoOptions options)
        {
            return RunWithConsoleCaptureAsync(() => Info.Action(options));
        }

        private Task RunWithConsoleCaptureAsync(Action action)
        {
            return Task.Run(() =>
            {
                var originalOut = Console.Out;
                var originalError = Console.Error;

                using (var writer = new CallbackTextWriter(_logger))
                {
                    try
                    {
                        Console.SetOut(writer);
                        Console.SetError(writer);
                        action();
                    }
                    finally
                    {
                        Console.SetOut(originalOut);
                        Console.SetError(originalError);
                    }
                }
            });
        }

        private sealed class CallbackTextWriter : TextWriter
        {
            private readonly Action<string> _logger;
            private readonly StringBuilder _lineBuffer = new StringBuilder();

            public CallbackTextWriter(Action<string> logger)
            {
                _logger = logger;
            }

            public override Encoding Encoding => Encoding.UTF8;

            public override void Write(char value)
            {
                if (value == '\r')
                {
                    return;
                }

                if (value == '\n')
                {
                    FlushLine();
                    return;
                }

                _lineBuffer.Append(value);
            }

            public override void Write(string value)
            {
                if (value == null)
                {
                    return;
                }

                foreach (var ch in value)
                {
                    Write(ch);
                }
            }

            public override void WriteLine(string value)
            {
                Write(value);
                FlushLine();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing && _lineBuffer.Length > 0)
                {
                    FlushLine();
                }

                base.Dispose(disposing);
            }

            private void FlushLine()
            {
                _logger(_lineBuffer.ToString());
                _lineBuffer.Clear();
            }
        }
    }
}
