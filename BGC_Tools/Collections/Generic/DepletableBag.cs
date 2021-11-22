using System.Collections;
using BGC.Mathematics;

namespace BGC.Collections.Generic;

/// <summary>
/// An unstable-sort set structure that acts as a select-without-replace bag.
/// </summary>
public sealed class DepletableBag<T> : IDepletable<T>
{
    private readonly List<T> values;
    private int availableCount;

    private readonly Random randomizer;

    public DepletableBag(
        bool autoRefill = false,
        Random? randomizer = null)
    {
        values = new List<T>();
        availableCount = 0;
        AutoRefill = autoRefill;

        if (randomizer is null)
        {
            randomizer = new Random(CustomRandom.Next());
        }

        this.randomizer = randomizer;
    }

    public DepletableBag(
        IEnumerable<T> values,
        bool autoRefill = false,
        Random? randomizer = null)
    {
        this.values = new List<T>(values);
        availableCount = this.values.Count;
        AutoRefill = autoRefill;

        if (randomizer is null)
        {
            randomizer = new Random(CustomRandom.Next());
        }

        this.randomizer = randomizer;
    }

    #region IDepletable<T>

    public bool AutoRefill { get; set; }
    public int TotalCount => values.Count;

    public T? PopNext()
    {
        if (availableCount == 0)
        {
            if (AutoRefill)
            {
                availableCount = values.Count;
            }
            else
            {
                Debug.LogWarning("Bag is empty and you tried to pull out an element.");
                return default;
            }
        }

        int index = randomizer.Next(0, availableCount);
        T temp = values[index];

        //Swap the position of the highest available value with the randomly chosen index.
        values[index] = values[availableCount - 1];
        values[availableCount - 1] = temp;

        //Decrement availableCount. Now that we have swapped those values,
        //all unusedValues will be below unusedCount.
        availableCount--;

        //return our chosen value.
        return temp;
    }

    public bool TryPopNext(out T? value)
    {
        if (availableCount == 0)
        {
            if (AutoRefill)
            {
                availableCount = values.Count;
            }
            else
            {
                value = default;
                return false;
            }
        }

        int index = randomizer.Next(0, availableCount);
        value = values[index];

        //Swap the position of the highest available value with the randomly chosen index.
        values[index] = values[availableCount - 1];
        values[availableCount - 1] = value;

        //Decrement availableCount. Now that we have swapped those values,
        //all unusedValues will be below unusedCount.
        availableCount--;

        //return our chosen value.
        return true;
    }

    /// <summary>
    /// Fills the bag back up.
    /// </summary>
    public void Reset()
    {
        availableCount = values.Count;
    }

    public bool DepleteValue(T value)
    {
        const int NOT_FOUND = -1;
        int index = NOT_FOUND;

        for (int i = 0; i < availableCount; i++)
        {
            if (values[i]!.Equals(value))
            {
                index = i;
                break;
            }
        }

        if (index == NOT_FOUND)
        {
            return false;
        }

        T temp = values[index];

        values[index] = values[availableCount - 1];
        values[availableCount - 1] = temp;

        availableCount--;

        return true;
    }

    public bool DepleteAllValue(T value)
    {
        bool success = false;

        while (DepleteValue(value))
        {
            success = true;
        }

        return success;
    }

    public bool ReplenishValue(T value)
    {
        const int NOT_FOUND = -1;
        int index = NOT_FOUND;

        for (int i = availableCount; i < TotalCount; i++)
        {
            if (values[i]!.Equals(value))
            {
                index = i;
                break;
            }
        }

        if (index == NOT_FOUND)
        {
            return false;
        }

        T temp = values[index];

        values[index] = values[availableCount];
        values[availableCount] = temp;

        availableCount++;

        return true;
    }

    public bool ReplenishAllValue(T value)
    {
        bool success = false;

        while (ReplenishValue(value))
        {
            success = true;
        }

        return success;
    }

    public bool ContainsAnywhere(T value) => values.Contains(value);

    public IList<T> GetAvailable() => values.GetRange(0, availableCount);

    public void CopyAllTo(T[] array, int arrayIndex)
    {
        values.CopyTo(
            index: 0,
            array: array,
            arrayIndex: arrayIndex,
            count: Math.Min(values.Count, array.Length - arrayIndex));
    }

    #endregion IDepletable<T>
    #region ICollection<T>

    public int Count => availableCount;

    bool ICollection<T>.IsReadOnly => false;

    public void Add(T value)
    {
        if (availableCount < values.Count)
        {
            values.Add(values[availableCount]);
            values[availableCount] = value;
        }
        else
        {
            values.Add(value);
        }
        availableCount++;
    }

    public void Clear()
    {
        values.Clear();
        availableCount = 0;
    }

    public bool Contains(T value)
    {
        for (int i = 0; i < availableCount; i++)
        {
            if (values[i]!.Equals(value))
            {
                return true;
            }
        }

        return false;
    }

    public void CopyTo(T[] dest, int destIndex)
    {
        values.CopyTo(
            index: 0,
            array: dest,
            arrayIndex: destIndex,
            count: Math.Min(availableCount, dest.Length - destIndex));
    }

    public bool Remove(T item)
    {
        int index = values.IndexOf(item);

        if (index > -1)
        {
            values.RemoveAt(index);

            if (index < availableCount)
            {
                availableCount--;
            }

        }

        return index != -1;
    }

    #endregion ICollection<T>
    #region IEnumerable<T>

    public IEnumerator<T> GetEnumerator() => values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => values.GetEnumerator();

    #endregion IEnumerable<T>
}
