namespace PlayingWithDistributedLock.Implementation;

public class LockFactoryException : Exception
{
    public LockFactoryException()
    {
    }

    public LockFactoryException(string message) : base(message)
    {
    }

    public LockFactoryException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
