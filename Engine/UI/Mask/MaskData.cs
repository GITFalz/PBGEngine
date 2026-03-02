using System.Diagnostics.CodeAnalysis;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.UI;

namespace PBG.Rendering.Mask
{
    public class MaskData
    {
        public List<UIMaskStruct> MaskStructs = [];
        public Dictionary<UI.IUICollection, int> Collections = [];

        public SSBO<UIMaskStruct> MaskSSBO = new([]);

        private BufferEnum _bufferUpdateState = BufferEnum.None;

        private UIController _controller;

        public MaskData(UIController controller)
        {
            _controller = controller;
        }

        public UIMaskStruct AddElement(UI.IUICollection collection, Vector2 topLeft, Vector2 bottomRight)
        {
            if (Collections.TryGetValue(collection, out int value))
            {
                var maskData = MaskStructs[value];
                maskData.TopLeft = topLeft;
                maskData.BottomRight = bottomRight;
                MaskStructs[value] = maskData;
                collection.MaskIndex = value;
                SetBufferUpdateState(BufferEnum.Update);
                return maskData;
            }

            var maskStruct = new UIMaskStruct()
            {
                TopLeft = topLeft,
                BottomRight = bottomRight
            };

            collection.MaskIndex = Collections.Count;
            Collections.Add(collection, Collections.Count);
            MaskStructs.Add(maskStruct);

            SetBufferUpdateState(BufferEnum.Recreate);
            return maskStruct;
        }

        public void RemoveElement(UI.IUICollection collectionToRemove)
        {
            if (!Collections.TryGetValue(collectionToRemove, out int index))
                return;

            MaskStructs.RemoveAt(index);
            Collections.Remove(collectionToRemove);

            foreach (var (collection, i) in Collections)
            {
                if (i > index)
                {
                    Collections[collection]--;
                    collection.UpdateMaskIndices();
                }
            }

            SetBufferUpdateState(BufferEnum.Recreate);
        }

        public bool GetMask(int maskIndex, [NotNullWhen(true)] out UIMaskStruct? mask)
        {
            if (maskIndex < 0 || maskIndex >= MaskStructs.Count)
            {
                mask = null;
                return false;
            }

            mask = MaskStructs[maskIndex];
            return true;
        }

        public void UpdateTransform(UI.IUICollection collection, Vector2 topLeft, Vector2 bottomRight)
        {
            if (!Collections.TryGetValue(collection, out int index))
                return;

            var maskData = MaskStructs[index];
            maskData.TopLeft = topLeft;
            maskData.BottomRight = bottomRight;
            MaskStructs[index] = maskData;

            SetBufferUpdateState(BufferEnum.Update);
        }

        public void UpdateScale(UI.IUICollection collection, Vector2 topLeft, Vector2 bottomRight)
        {
            UpdateTransform(collection, topLeft, bottomRight);
        }

        public void Update()
        {
            if (_bufferUpdateState != BufferEnum.None)
            {
                UpdateBuffer();
                _bufferUpdateState = BufferEnum.None;
            }
        }

        public void UpdateBuffer()
        {

            switch (_bufferUpdateState)
            {
                case BufferEnum.Recreate:
                    MaskSSBO.Renew([..MaskStructs]);
                    _controller.UIMesh.Descriptor.BindSSBO(MaskSSBO, 2);
                    _controller.TextMesh.Descriptor.BindSSBO(MaskSSBO, 3);
                    break;
                case BufferEnum.Update:
                    MaskSSBO.Update([..MaskStructs]);
                    break;
                case BufferEnum.None:
                    break;
            }
        }

        public void Delete()
        {
            MaskStructs = [];
            Collections = [];
            
            MaskSSBO.Dispose();
        }

        public void SetBufferUpdateState(BufferEnum state)
        {
            if ((int)_bufferUpdateState < (int)state)
            {
                _bufferUpdateState = state;
            }
        } 
    }

    public struct UIMaskStruct
    {
        public Vector2 TopLeft;
        public Vector2 BottomRight;

        public Vector2 Size => BottomRight - TopLeft;

        public override string ToString()
        {
            return $"TopLeft: {TopLeft}, BottomRight: {BottomRight}";
        }
    }
}