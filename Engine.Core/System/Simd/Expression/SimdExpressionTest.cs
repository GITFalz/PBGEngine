using System.Linq.Expressions;
using System.Runtime.Intrinsics.X86;
using System.Text;
using Microsoft.VisualBasic;

namespace PBG;

[InternalSystemInit(InitPriority.Data)]
public unsafe static class SimdExpressionTest
{
    public static void Init()
    {
        // The point of the test is to take all the numbers from array1 and put them into array2 using 2 different Ops

        // Scalar version
        {
            float[] array1 = [ 1, 2, 3, 4, 5, 6, 7, 8, 9,10,11,12,13,14,15,16 ];
            float[] array2 = [ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 ]; 

            for (int i = 0; i < 16; i++)
            {
                float loaded = array1[i];
                array2[i] = loaded;
            }

            /*
            var o = new ScalarContext();

            o.ForElements(0, 16, 1, i =>
            {
                var loaded = Load(array1, i);
                Store(array2, i, loaded);
            });
            */

            Console.WriteLine("Scalar array:");
            PrintArray(array2);
        }

        // Avx2 version
        {
            float[] array1 = [ 1, 2, 3, 4, 5, 6, 7, 8, 9,10,11,12,13,14,15,16 ];
            float[] array2 = [ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 ]; 

            fixed (float* ptr1 = array1)
            fixed (float* ptr2 = array2)
            {
                for (int i = 0; i < 16; i+=8)
                {
                    V256f loaded = Avx2.LoadVector256(ptr1 + i);
                    Avx2.Store(ptr2 + i, loaded);
                }
            }

            Console.WriteLine("Avx2 array:");
            PrintArray(array2);
        }


        var o = new ScalarContext();

        /*
        var a = o.New(1);
        var b = o.New(2);
        var c = a + b;
        */

        var a = Expression.Constant(V256I.One);
        var b = Expression.Constant(V256I.Two);
        var c = Expression.Add(a, b);

        var block = Expression.Block([
            c
        ]);

        var lambda = Expression.Lambda<Func<V256i>>(block);
        Console.WriteLine(Dump(lambda));
        
        Func<V256i> compiled = lambda.Compile();
        Console.WriteLine(compiled());
    }

    static string Dump(Expression? expr, int indent = 0)
    {
        if (expr == null) return "";
        var pad = new string(' ', indent * 2);
        var sb = new StringBuilder();
        sb.AppendLine($"{pad}{expr.NodeType} : {expr.Type.Name}");

        switch (expr)
        {
            case BinaryExpression b:
                sb.Append(Dump(b.Left, indent + 1));
                sb.Append(Dump(b.Right, indent + 1));
                break;
            case MethodCallExpression m:
                sb.AppendLine($"{pad}  Method: {m.Method.DeclaringType?.Name}.{m.Method.Name}");
                foreach (var a in m.Arguments) sb.Append(Dump(a, indent + 1));
                break;
            case UnaryExpression u:
                sb.Append(Dump(u.Operand, indent + 1));
                break;
            case ConstantExpression c:
                sb.AppendLine($"{pad}  Value: {c.Value}");
                break;
            case ParameterExpression p:
                sb.AppendLine($"{pad}  Name: {p.Name}");
                break;
            case BlockExpression blk:
                foreach (var e in blk.Expressions) sb.Append(Dump(e, indent + 1));
                break;
            case LambdaExpression l:
                sb.Append(Dump(l.Body, indent + 1));
                break;
        }
        return sb.ToString();
    }


    private static void PrintArray<T>(T[] array) where T : unmanaged
    {
        string s = "[";
        for (int i = 0; i < 16; i++)
        {
            if (i > 0) s += ", ";
            s += array[i].ToString();
        }
        s += "]";

        Console.WriteLine(s);
    }


    public abstract class AOpsContext<T> where T : IOps
    {
        public ExecutionList MainList;
        public ExecutionList CurrentList;

        public AOpsContext()
        {
            MainList = [];
            CurrentList = MainList;
        }

        /*
        public void ForElements(int start, int end, int increment, Action<IOps> value)
        {
            ForLoop forLoop = new();

            CurrentList.Add(forLoop);
        }
        
        public void ForRange(int start, int end, int increment, Action<IOps> value)
        {
            
        }
        */


    }



    public class ExecutionList : List<IExecute> {}



    public interface IExecute
    {
        public void Execute();
    }

    

    public class ForLoop : IExecute
    {
        public ExecutionList List = [];
        
        public int Start;
        public int End;
        public int Increment;




        public void Execute()
        {
            throw new NotImplementedException();
        }
    }





    public class ScalarContext : AOpsContext<ScalarOps>
    {
        public IntOps New(int value)
        {
            return new IntOps(value);
        }
    }

    public class V256Context : AOpsContext<V256Ops>
    {
        public V256iOps New(int value)
        {
            return new V256iOps(value);
        }
    }


    /*
    BlockExpression loop = Expression.Block(
        new[] { i },                                   // locals declared in this block
        Expression.Assign(i, Expression.Constant(0)),  // i = 0

        Expression.Loop(
            Expression.Block(
                Expression.IfThen(
                    Expression.GreaterThanOrEqual(i, n),
                    Expression.Break(breakLabel)
                ),

                // ---- loop body goes here ----
                Expression.Call(
                    typeof(Console), nameof(Console.WriteLine), null,
                    Expression.Convert(i, typeof(object))
                ),
                // ------------------------------

                Expression.PostIncrementAssign(i)      // i++
            ),
            breakLabel
        )
    );
    */


    public interface IOps
    {
        //public static IOps operator +(IOps left, IOps right) {}
    }

    public abstract class ScalarOps : IOps
    {
        
    }

    public class IntOps : ScalarOps
    {
        public int Value;

        public IntOps(int value)
        {
            Value = value;
        }
    }

    public abstract class V256Ops : IOps
    {
        
    }

    public class V256iOps : V256Ops
    {
        public V256i Value;

        public V256iOps(int value)
        {
            Value = V256I.New(value);
        }
    }
}