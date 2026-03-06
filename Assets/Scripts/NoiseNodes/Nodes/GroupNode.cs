using System.Linq.Expressions;
using PBG.Voxel;

namespace PBG.Assets.Scripts.NoiseNodes.Nodes
{
    public class GroupNode : NoiseNode
    {
        public NoiseValue[] Variables;
        public List<NoiseNode> ActionMap = [];

        public GetterValue[] InternalGetters;
        public SetterValue[] InternalSetters;

        public GroupNode(NoiseValue[] variables, List<NoiseNode> actionMap, GetterValue[] getters, GetterValue[] internalGetters, SetterValue[] setters, SetterValue[] internalSetters) : base(getters, setters,"")
        {
            Variables = variables;
            ActionMap = actionMap;
            InternalGetters = internalGetters;
            InternalSetters = internalSetters; 
        }
        
        public override void Basic(int x, int y)
        {
            /*
            for (int i = 0; i < InternalSetters.Length; i++)
            {
                InternalSetters[i].SetValue(Getters[i].GetValue());
            }

            for (int i = 0; i < ActionMap.Count; i++)
            {
                var action = ActionMap[i];
                action.Basic(x, y);
            }

            for (int i = 0; i < InternalGetters.Length; i++)
            {
                Setters[i].SetValue(InternalGetters[i].GetValue());
            }
            */
        }

        public override void Run(NoiseNodeManager manager, VoxelChunk chunk, int x, int y)
        {
            /*
            for (int i = 0; i < InternalSetters.Length; i++)
            {
                var value = Getters[i].GetValue();
                InternalSetters[i].SetValue(value);
            }

            for (int i = 0; i < ActionMap.Count; i++)
            {
                var action = ActionMap[i];
                action.Run(manager, chunk, x, y);
            }

            for (int i = 0; i < InternalGetters.Length; i++)
            {
                var value = InternalGetters[i].GetValue();
                Setters[i].SetValue(value);
            }
            */
        }

        public override void LOD(NoiseNodeManager manager, LODChunk chunk, int level, int x, int y)
        {
            /*
            for (int i = 0; i < InternalSetters.Length; i++)
            {
                var value = Getters[i].GetValue();
                InternalSetters[i].SetValue(value);
            }

            for (int i = 0; i < ActionMap.Count; i++)
            {
                var action = ActionMap[i];
                action.LOD(manager, chunk, level, x, y);
            }

            for (int i = 0; i < InternalGetters.Length; i++)
            {
                var value = InternalGetters[i].GetValue();
                Setters[i].SetValue(value);
            }
            */
        }

        public override NoiseNode Copy(NoiseValue[] variables)
        {
            NoiseValue[] internalVariables = [..variables];

            Copy(variables, out var getters, out var setters);  
            InternalCopy(internalVariables, out var internalGetters, out var internalSetters);
            
            var groupNode = new GroupNode(internalVariables, [], getters, internalGetters, setters, internalSetters);
            for (int i = 0; i < ActionMap.Count; i++)
            {
                var action = ActionMap[i];
                groupNode.ActionMap.Add(action.Copy(internalVariables));
            }
            return groupNode;
        }

        public void InternalCopy(NoiseValue[] variables, out GetterValue[] getters, out SetterValue[] setters)
        {
            getters = new GetterValue[InternalGetters.Length];
            setters = new SetterValue[InternalSetters.Length];

            /*
            for (int i = 0; i < InternalGetters.Length; i++)
            {
                var getter = InternalGetters[i];
                getters[i] = getter.Copy(variables);
            }

            for (int i = 0; i < InternalSetters.Length; i++)
            {
                var setter = InternalSetters[i];
                setters[i] = setter.Copy(variables);
            }
            */
        }
        protected override Expression BuildBasicExpression() => BuildExpression();
        protected override Expression BuildTerrainExpression() => BuildExpression();
        private Expression BuildExpression()
        {
            return Const(0);
        }
    }
}