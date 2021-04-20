using System.Collections.Generic;

public class AverageCalculator
{
    bool updated = false;

    int lastValue = 0;

    int numberCapacity;

    int care = 0;

    private List<int> allNumbers = new List<int>();

    public AverageCalculator (int _numberCapacity)
    {
        numberCapacity = _numberCapacity;
    }

    public void Put (int integer)
    {
        if (allNumbers.Count == numberCapacity)
        {
            allNumbers[care] = integer;

            care = care < allNumbers.Count ? care + 1 : 0;
        }

        allNumbers.Add(integer);

        updated = true;
    }

    public int Value ()
    {
        if (!updated) return lastValue;

        updated = false;

        var count = allNumbers.Count;

        int all = 0;

        for (int i = 0; i < count; ++i)
        {
            all += allNumbers[i];
        }

        lastValue = all/count;

        return lastValue;
    }

}