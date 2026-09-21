using System;
using System.Collections.Generic;
using System.Linq;
using TranSimCS.Collections;
using Xunit;

namespace TranSimCSTests;

public class ScratchArrayTests {
    [Fact]
    public void GetArray_WithinCapacity_ReusesArray() {
        var scratch = new ScratchArray<int>(4);

        var first = scratch.GetArray(2);
        first[0] = 123;

        var second = scratch.GetArray(4);

        Assert.Same(first, second);
        Assert.Equal(123, second[0]);
    }

    [Fact]
    public void GetArray_EqualToCapacity_ReusesArray() {
        var scratch = new ScratchArray<int>(4);

        var first = scratch.GetArray(4);
        var second = scratch.GetArray(4);

        Assert.Same(first, second);
    }

    [Fact]
    public void GetArray_AboveCapacity_Reallocates() {
        var scratch = new ScratchArray<int>(4);

        var first = scratch.GetArray(4);
        var second = scratch.GetArray(5);

        Assert.NotSame(first, second);
        Assert.True(second.Length >= 5);
    }

    [Fact]
    public void GetArray_GrowsExponentially() {
        var scratch = new ScratchArray<int>(4);

        var array = scratch.GetArray(9);

        Assert.Equal(16, array.Length);
    }

    [Fact]
    public void GetArray_ReallocationDoesNotPreserveOldContents() {
        var scratch = new ScratchArray<int>(4);

        var array = scratch.GetArray(4);
        array[0] = 123;

        var larger = scratch.GetArray(5);

        Assert.NotSame(array, larger);
        Assert.Equal(0, larger[0]);
    }

    [Fact]
    public void GetArray_ZeroLength_ReturnsArray() {
        var scratch = new ScratchArray<int>(4);

        var array = scratch.GetArray(0);

        Assert.NotNull(array);
        Assert.Equal(4, array.Length);
    }

    [Fact]
    public void Constructor_UsesSpecifiedCapacity() {
        var scratch = new ScratchArray<int>(32);

        Assert.Equal(32, scratch.GetArray(0).Length);
    }
}