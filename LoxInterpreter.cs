using System;

namespace Lox
{
    sealed class LoxInterpreter
    {
        private bool _hadError;
      
        public bool Run(string source)
        {
            var scanner = new Scanner();
            scanner.ScanTokens(source);

            foreach (var error in scanner.GetErrors())
            {
                Report(error.Line, "", error.Message);
            }

            foreach (var token in scanner.GetTokens())
            {
                Console.WriteLine(token);
            }

            return _hadError;
        }

        private void Report(int line, string where, string message)
        {
            Console.Error.WriteLine($"[line {line} ] Error {where} : {message}");
            _hadError = true;
        }
    }
}
