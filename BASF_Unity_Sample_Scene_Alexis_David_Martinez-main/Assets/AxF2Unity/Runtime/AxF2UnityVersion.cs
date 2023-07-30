// Copyright (c) 2021 Steinbeis-Forschungszentrum Computer Graphik und Digitalisierung

using System;

namespace AxF2Unity
{
    public class AxF2UnityVersion
    {
        public const int major = 1;
        public const int minor = 2;
        public const int patch = 1;

        public static string versionString
        {
            get
            {
                return String.Format("{0:D}.{1:D}.{2:D}", major, minor, patch) + "-BASF";
            }
        }

        public static bool IsOlderThan(int testMajor, int testMinor, int testPatch, int refMajor = major, int refMinor = minor, int refPatch = patch)
        {
            if (testMajor < refMajor)
                return true;
            else if (testMajor > refMajor)
                return false;

            if (testMinor < refMinor)
                return true;
            else if (testMinor > refMinor)
                return false;

            if (testPatch < refPatch)
                return true;
            else
                return false;
        }

        public static bool IsOlderThanEqual(int testMajor, int testMinor, int testPatch, int refMajor = major, int refMinor = minor, int refPatch = patch)
        {
            if (testMajor < refMajor)
                return true;
            else if (testMajor > refMajor)
                return false;

            if (testMinor < refMinor)
                return true;
            else if (testMinor > refMinor)
                return false;

            if (testPatch < refPatch)
                return true;
            else if (testPatch > refPatch)
                return false;

            return true;
        }
    }
}
