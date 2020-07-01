using System.Collections.Generic;

namespace Lox
{
    class LoxFunction : LoxCallable
    {
        private FunctionStatement _declaration;
        private Environment _closure;

        public LoxFunction(FunctionStatement declaration, Environment closure)
        {
            _declaration = declaration;
            _closure = closure;
        }

        public int Arity => _declaration.Parameters.Count;

        public object Call(Evaluator evaluator, List<object> arguments)
        {
            var env = new Environment(_closure);
            for(int i = 0; i < _declaration.Parameters.Count; i++)
            {
                env.Define(_declaration.Parameters[i].Lexeme, arguments[i]);
            }
            try 
            {
                evaluator.EvaluateBlock(_declaration.Body, env);
            }
            catch (Return ret)
            {
                return ret.Value;
            }
            return null;
        }

        public override string ToString()
        {
            return "<fn " + _declaration.Name.Lexeme + ">";
        }
    }
}
