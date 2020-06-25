using System;

namespace Lox
{
    abstract class Option<T>
    {
      
        public static implicit operator Option<T>(T value)
        {
            return new Some<T>(value);
        }
        public static implicit operator Option<T>(Optional none)
        {
            return new None<T>();
        }

        public abstract Option<TResult> Map<TResult>(Func<T, TResult> map);
        public abstract T Or(T whenNone);

    }

    public class Optional
    {
        public static Optional None { get; } = new Optional();
        private Optional() { }

    }

    class Some<T> : Option<T>
    {
        public T Value { get; }

        public Some(T value)
        {
            Value = value;
        }

        public override Option<TResult> Map<TResult>(Func<T, TResult> map)
        {
            return map(Value);
        }
        public override T Or(T whenNone)
        {
            return Value;
        }

    }

    class None<T> : Option<T>
    {
        public override Option<TResult> Map<TResult>(Func<T, TResult> map)
        {
            return Optional.None;
        }
        public override T Or(T whenNone)
        {
            return whenNone;
        }
    }

}
