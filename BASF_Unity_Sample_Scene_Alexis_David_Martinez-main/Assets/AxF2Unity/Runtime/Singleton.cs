// Copyright (c) 2021 Steinbeis-Forschungszentrum Computer Graphik und Digitalisierung

using System;

namespace AxF2Unity
{
    public class Singleton<T> where T : new()
    {
        private static readonly Lazy<T> s_Instance = new Lazy<T>(() => new T());

        public static T instance => s_Instance.Value;
    }
}
