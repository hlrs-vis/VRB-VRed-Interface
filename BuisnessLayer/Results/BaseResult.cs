using System;

namespace BuisnessLayer.Results
{
    public class BaseResult
	{
		public BaseResult(Exception innerException = null)
		{
			InnerException = innerException;
		}
		public Exception InnerException { get; }
		public bool HasException => InnerException != null;
	}

	public class ValueResult<T> : BaseResult
	{
		public T Value { get; set; }

		public ValueResult(Exception inException = null) : base(inException)
		{
			
		}

		public ValueResult(T value)
		{
			Value = value;
		}

		public ValueResult(T value, Exception inException = null) : base(inException)
		{
			Value = value;
		}
	} 
}
