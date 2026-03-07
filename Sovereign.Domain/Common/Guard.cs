
using System;

namespace Sovereign.Domain.Common;

public static class Guard
{
    public static void AgainstNull(object? input, string name)
    {
        if (input is null)
            throw new ArgumentNullException(name);
    }

    public static void AgainstNegative(double value, string name)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(name);
    }
}
