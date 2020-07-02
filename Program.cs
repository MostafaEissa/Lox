using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace Lox
{
    class Program
    {
        static void Main(string[] args)
        {
            switch (args.Length)
            {
                case 0: 
                    RunPrompt();     
                    break;
                case 1:
                    RunFile(args[0]);
                    break;
                default:
                    Console.WriteLine("Usage: lox [script]");
                    System.Environment.Exit(64);
                    break;

            }
            Console.ReadLine();
        }

        static void RunFile(string path)
        {
            var source = File.ReadAllText(path);
            var interpreter = new LoxInterpreter();
            var hadErrors = interpreter.Run(source);

            if (hadErrors)
                System.Environment.Exit(62);
        }

        static void RunPrompt()
        {
            var interpreter = new LoxInterpreter();
            while (true)
            {
                Console.Write("> ");
                interpreter.Run(Console.ReadLine());
            }
        }

    }
}
