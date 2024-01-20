using System;
using System.Reactive.Linq;

namespace WireMockInspector;

public static class ReactiveEx
{
    public static IObservable<T> DiscardExceptions<T>(this IObservable<T> observable) 
        => observable.Catch(Observable.Empty<T>());
}