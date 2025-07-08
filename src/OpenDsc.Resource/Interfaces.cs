// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Resource;

public interface IDscResource<T>
{
    string GetSchema();
    string ToJson(T instance);
    T Parse(string json);
}

public interface IGettable<T> : IDscResource<T>
{
    T Get(T instance);
}

public interface ISettable<T> : IDscResource<T>
{
    SetResult<T>? Set(T instance);
}

public interface ISettableWhatIf<T> : ISettable<T>
{
    SetResult<T> SetWhatIf(T instance);
}

public interface IDeletable<T> : IDscResource<T>
{
    void Delete(T instance);
}

public interface ITestable<T> : IDscResource<T>
{
    TestResult<T> Test(T instance);
}

public interface IExportable<T> : IDscResource<T>
{
    IEnumerable<T> Export();
}
