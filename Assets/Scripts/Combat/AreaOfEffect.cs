using PBG.MathLibrary;

public abstract class AreaOfEffect
{  
    public abstract bool InArea(Vector3 affectedPosition, Vector3 effectPosition);
    public Vector3 GetEffect(Vector3 affectedPosition, Vector3 effectPosition) => GetEffect(affectedPosition, effectPosition, (0, 0, 0));
    public abstract Vector3 GetEffect(Vector3 affectedPosition, Vector3 effectPosition, Vector3 defaultDirection);
}

public class SphereEffect : AreaOfEffect
{
    public bool Outwards;
    public float Radius;
    public float Force;

    public SphereEffect(float radius, float force, bool outwards = true)
    {
        Outwards = outwards;
        Radius = radius;
        Force = force;
    }

    public override bool InArea(Vector3 affectedPosition, Vector3 effectPosition) => (affectedPosition - effectPosition).Length <= Radius;

    public override Vector3 GetEffect(Vector3 affectedPosition, Vector3 effectPosition, Vector3 defaultDirection)
    {
        Vector3 direction = affectedPosition - effectPosition;
        float distance = direction.Length;

        if (distance > 0f)
            direction /= distance;
        else
            direction = defaultDirection;

        float t = Math.Min(distance / Radius, 1f);

        float appliedForce;
        if (Outwards)
        {
            appliedForce = Mathf.Lerp(0f, Force, t);
        }
        else
        {
            appliedForce = Mathf.Lerp(Force, 0f, t);
        }

        return direction * appliedForce;
    }
}

public class CylinderEffect : AreaOfEffect
{
    public bool Outwards;
    public float Radius;
    public float Height;
    public float Force;

    public CylinderEffect(float radius, float height, float force, bool outwards = true)
    {
        Outwards = outwards;
        Radius = radius;
        Height = height;
        Force = force;
    }

    public override bool InArea(Vector3 affectedPosition, Vector3 effectPosition)
    {
        Vector3 horizontalDir = affectedPosition - effectPosition;
        horizontalDir.Y = 0f;
        float horizontalDistance = horizontalDir.Length;
        float verticalDistance = Math.Abs(affectedPosition.Y - effectPosition.Y);
        return horizontalDistance <= Radius && verticalDistance <= Height / 2f;
    }

    public override Vector3 GetEffect(Vector3 affectedPosition, Vector3 effectPosition, Vector3 defaultDirection)
    {
        Vector3 horizontalDir = affectedPosition - effectPosition;
        horizontalDir.Y = 0f;

        float horizontalDistance = horizontalDir.Length;

        if (horizontalDistance > 0f)
            horizontalDir /= horizontalDistance;
        else
            horizontalDir = defaultDirection;

        float verticalDistance = Math.Abs(affectedPosition.Y - effectPosition.Y);
        if (verticalDistance > Height / 2f)
            return Vector3.Zero; // Outside cylinder height

        float t = Math.Min(horizontalDistance / Radius, 1f);

        float appliedForce;
        if (Outwards)
            appliedForce = Mathf.Lerp(0f, Force, t);
        else
            appliedForce = Mathf.Lerp(Force, 0f, t);

        Vector3 direction = horizontalDir;
        direction.Y = (affectedPosition.Y - effectPosition.Y) != 0f ? 
                       Math.Sign(affectedPosition.Y - effectPosition.Y) : 0f;

        return direction * appliedForce;
    }
}
