namespace GDF.Util;

public struct Accumulator
{
    private float _amount;

    public void Add(float amount = 1)
    {
        _amount += amount;
    }

    public bool Consume(float amount = 1)
    {
        bool canConsume = _amount >= amount;
        if (canConsume)
        {
            _amount -= amount;
        }
        return canConsume;
    }

    public void Reset() {
        _amount = 0;
    }
}