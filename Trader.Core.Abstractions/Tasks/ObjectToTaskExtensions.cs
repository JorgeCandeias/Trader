namespace System;

public static class ObjectToTaskExtensions
{
    public static Task<T> AsTaskResult<T>(this T obj)
    {
        return Task.FromResult(obj);
    }
}