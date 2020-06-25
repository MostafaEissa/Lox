using System;
using System.Linq;

namespace Lox
{
    sealed class LoxInterpreter
    {
        private bool _hadError;
      
        public bool Run(string source)
        {
            var scanner = new Scanner(source);
            scanner.ScanTokens();

            var parser = new Parser(scanner.GetTokens().ToList());
            var result = parser.Parse();

            foreach (var error in scanner.GetErrors())
            {
                Report(error.Line, error.Where , error.Message);
            }

            foreach (var error in parser.GetErrors())
            {
                Report(error.Line, error.Where,  error.Message);
            }

            if (!_hadError)
                Console.WriteLine(new AbstractSyntaxTreePrinter().Print(result));

            return _hadError;
        }

        private void Report(int line, string where, string message)
        {
            Console.Error.WriteLine($"[line {line} ] Error {where} : {message}");
            _hadError = true;
        }
    }
}
