
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.UI;

namespace PBG.Rendering.Meshes
{
    public class UIMesh
    {
        public int ElementCount = 0;
        public int VisibleElementCount = 0;

        public HashSet<IUIPanel> PanelStructsToBeRemoved = [];
        public HashSet<IUIPanel> PanelStructsToBeAdded = [];
        public UIPanelStruct[] PanelStructs = [];
        public Dictionary<UI.IUIPanel, int> Panels = [];

        //private Graphics.VAO _vao = new();
        private SSBO<UIPanelStruct> _uiSSBO = new([]);

        private bool _updateVisibility = false;
        private BufferEnum _bufferUpdateState = BufferEnum.None;

        private UIController _controller;
        public readonly Descriptor Descriptor;

        public UIMesh(UIController controller)
        {
            _controller = controller;
            Descriptor = controller.UIData.GetUiDescriptor();
            Descriptor.BindSSBO(_uiSSBO, 1);
            Descriptor.BindSSBO(controller.MaskData.MaskSSBO, 2);
        }

        public void AddElement(UI.IUIPanel panel)
        {
            PanelStructsToBeAdded.Add(panel);
            SetBufferUpdateState(BufferEnum.Recreate);
        }

        public void RemoveElement(UI.IUIPanel panelToRemove)
        {
            PanelStructsToBeRemoved.Add(panelToRemove);
            SetBufferUpdateState(BufferEnum.Recreate);
        }

        public void UpdateMaskIndex(UI.IUIPanel panel, int index)
        {
            if (Panels.TryGetValue(panel, out int panelIndex))
            {
                var panelData = PanelStructs[panelIndex];
                panelData.Data.W = index;
                PanelStructs[panelIndex] = panelData;
            }
            SetBufferUpdateState(BufferEnum.Update);
        }

        public void UpdateTextureIndex(UI.IUIPanel panel)
        {
            if (Panels.TryGetValue(panel, out int panelIndex))
            {
                var panelData = PanelStructs[panelIndex];
                panelData.Data.X = panel.TextureID;
                PanelStructs[panelIndex] = panelData;
            }
            SetBufferUpdateState(BufferEnum.Update);
        }

        public void QueueUpdateVisibility()
        {
            _updateVisibility = true;
            if (_bufferUpdateState != BufferEnum.Recreate)
                SetBufferUpdateState(BufferEnum.Update);
        }

        public void UpdateVisibility()
        {
            int i = 0;
            VisibleElementCount = 0;
            foreach (var (panel, index) in Panels)
            {
                //Console.WriteLine(panel.GetName() + " has a visibility of: " + panel.Visible + " at: " + index + " and: " + i);
                if (panel.IsValid)
                {
                    var data = PanelStructs[i];
                    if (index != data.Data.Z)
                    {
                        data.Data.Z = index;
                        PanelStructs[i] = data;
                    }
                    i++;
                    VisibleElementCount++;
                }
            }

            _updateVisibility = false;
        }

        public void Resize()
        {
            foreach (var (panel, index) in Panels)
            {
                var panelStruct = PanelStructs[index];
                panelStruct.SizeSlice = (panel.Size.X, panel.Size.Y, panel.Slice.X, panel.Slice.Y);
                panelStruct.Transform = panel.Transform;
                PanelStructs[index] = panelStruct;
            }
            _uiSSBO.Update(PanelStructs);
        }

        public void UpdateTransform(UI.IUIPanel panel)
        {
            Panels.TryGetValue(panel, out var index);

            var panelStruct = PanelStructs[index];
            panelStruct.Transform = panel.Transform;
            PanelStructs[index] = panelStruct;

            SetBufferUpdateState(BufferEnum.Update);
        }

        public void UpdateScale(UI.IUIPanel panel)
        {
            Panels.TryGetValue(panel, out var index);

            var panelStruct = PanelStructs[index];
            panelStruct.SizeSlice = (panel.Size.X, panel.Size.Y, panel.Slice.X, panel.Slice.Y);
            PanelStructs[index] = panelStruct;

            SetBufferUpdateState(BufferEnum.Update);
        }

        public void UpdateColor(UI.IUIPanel panel)
        {
            Panels.TryGetValue(panel, out var index);

            var panelStruct = PanelStructs[index];
            panelStruct.Color = panel.Color;
            PanelStructs[index] = panelStruct;

            _updateVisibility = true;
            if (_bufferUpdateState != BufferEnum.Recreate)
                SetBufferUpdateState(BufferEnum.Update);
        }

        public void UpdateBorderUI(UI.IUIPanel panel)
        {
            Panels.TryGetValue(panel, out var index);

            var panelStruct = PanelStructs[index];
            panelStruct.Border = panel.BorderUI;
            PanelStructs[index] = panelStruct;

            _updateVisibility = true;
            if (_bufferUpdateState != BufferEnum.Recreate)
                SetBufferUpdateState(BufferEnum.Update);
        }

        public void UpdateBorderColor(UI.IUIPanel panel)
        {
            Panels.TryGetValue(panel, out var index);

            var panelStruct = PanelStructs[index];
            panelStruct.BorderColor = panel.BorderColor;
            PanelStructs[index] = panelStruct;

            _updateVisibility = true;
            if (_bufferUpdateState != BufferEnum.Recreate)
                SetBufferUpdateState(BufferEnum.Update);
        }

        public void UpdateAnimationTranslation(UI.IUIPanel panel)
        {
            Panels.TryGetValue(panel, out var index);

            var panelStruct = PanelStructs[index];
            panelStruct.Translation = panel.AnimationTranslation;
            PanelStructs[index] = panelStruct;

            SetBufferUpdateState(BufferEnum.Update);
        }

        public void UpdateAnimationScale(UI.IUIPanel panel)
        {
            Panels.TryGetValue(panel, out var index);

            var panelStruct = PanelStructs[index];
            panelStruct.ScaleRotation.X = panel.AnimationScale;
            PanelStructs[index] = panelStruct;

            SetBufferUpdateState(BufferEnum.Update);
        }

        public void UpdateAnimationRotation(UI.IUIPanel panel)
        {
            Panels.TryGetValue(panel, out var index);

            var panelStruct = PanelStructs[index];
            panelStruct.ScaleRotation.Y = panel.AnimationRotation;
            PanelStructs[index] = panelStruct;

            SetBufferUpdateState(BufferEnum.Update);
        }


        public void Update()
        {
            
            if (_bufferUpdateState != BufferEnum.None)
            {
                UpdateBuffers();
                _bufferUpdateState = BufferEnum.None;
                _updateVisibility = false;
            }
        }

        private void UpdateBuffers()
        {
            switch (_bufferUpdateState)
            {
                case BufferEnum.Update:
                    if (_updateVisibility)
                        UpdateVisibility();

                    _uiSSBO.Update(PanelStructs);
                    break;
                case BufferEnum.Recreate:
                    foreach (var panel in PanelStructsToBeRemoved)
                        Panels.Remove(panel);

                    foreach (var panel in PanelStructsToBeAdded)
                        Panels.TryAdd(panel, 0);

                    ElementCount = 0;
                    PanelStructs = new UIPanelStruct[Panels.Count];
                    foreach (var (panel, _) in Panels)
                    {
                        Panels[panel] = ElementCount;
                        PanelStructs[ElementCount] = new UIPanelStruct
                        {
                            SizeSlice = (panel.Size.X, panel.Size.Y, panel.Slice.X, panel.Slice.Y),
                            Color = panel.Color,
                            Data = (panel.TextureID, 0, ElementCount, panel.MaskIndex),
                            Transform = panel.Transform,
                            BorderColor = panel.BorderColor,
                            Border = panel.BorderUI,
                            Translation = panel.AnimationTranslation,
                            ScaleRotation = (panel.AnimationScale, panel.AnimationRotation),
                        };  
                        ElementCount++;
                    }

                    PanelStructsToBeRemoved = [];
                    PanelStructsToBeAdded = [];

                    UpdateVisibility();

                    _uiSSBO.Renew(PanelStructs);
                    Descriptor.BindSSBO(_uiSSBO, 1);
                    break;
            }

        }

        public void SetBufferUpdateState(BufferEnum state)
        {
            if ((int)_bufferUpdateState < (int)state)
            {
                _bufferUpdateState = state;
            }
        }

        public void Render()
        {
            if (VisibleElementCount <= 0)
                return;

            Descriptor.Bind();
            /*
            _vao.Bind();
            _uiSSBO.Bind(0); 

            GL.DrawArrays(PrimitiveType.Triangles, 0, VisibleElementCount * 6);
            Shader.Error("UIMesh Error: ");

            _uiSSBO.Unbind();
            _vao.Unbind();
            */
            GFX.Draw((uint)VisibleElementCount * 6, 1, 0, 0);
        }

        public void Delete()
        {
            PanelStructs = [];
            Panels = [];
            
            //_vao.DeleteBuffer();
            _uiSSBO.Dispose();
        }
    }

    public struct UIPanelStruct
    {
        public Vector4 SizeSlice;
        public Vector4 Color;
        public Vector4i Data;
        public Vector4 Transform;

        // Border
        public Vector4 BorderColor;
        public Vector4 Border;

        // Animation
        public Vector2 Translation;
        public Vector2 ScaleRotation;

        public override string ToString()
        {
            return $"Size: {SizeSlice.Xy}, Slice: {SizeSlice.Zw}, Color: {Color}, Data: {Data}, Transform: {Transform}";
        }
    }
}