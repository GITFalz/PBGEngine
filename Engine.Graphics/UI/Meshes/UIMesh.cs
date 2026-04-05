
using PBG.Graphics;
using PBG.Graphics.Vulkan;
using PBG.Mathematics;
using PBG.Parse;
using PBG.UI;

namespace PBG.Rendering.Meshes
{
    public class UIMesh
    {
        public int ElementCount = 0;
        public int VisibleElementCount = 0;

        public HashSet<UIPanel> PanelStructsToBeRemoved = [];
        public HashSet<UIPanel> PanelStructsToBeAdded = [];
        public NewUIPanelStruct[] PanelStructs = [];
        public Vector4[] StyleData = [];
        public Dictionary<UIPanel, UIMetaData> Panels = [];

        //private Graphics.VAO _vao = new();

        private SSBO<NewUIPanelStruct> _uiSSBO = new([], true);
        private SSBO<Vector4> _styleSSBO = new([], true);

        private bool _updateVisibility = false;
        private BufferEnum _bufferUpdateState = BufferEnum.None;

        private UIController _controller;
        public readonly Descriptor Descriptor;

        public UIMesh(UIController controller)
        {
            _controller = controller;
            Descriptor = controller.UIData.GetUiDescriptor();
            Descriptor.BindSSBO(_uiSSBO, 1);
            Descriptor.BindSSBO(_styleSSBO, 2);
            Descriptor.BindSSBO(controller.MaskData.MaskSSBO, 3);
        }

        public void AddElement(UIPanel panel)
        {
            PanelStructsToBeAdded.Add(panel);
            SetBufferUpdateState(BufferEnum.Recreate);
        }

        public void RemoveElement(UIPanel panelToRemove)
        {
            PanelStructsToBeRemoved.Add(panelToRemove);
            SetBufferUpdateState(BufferEnum.Recreate);
        }

        public void UpdateMaskIndex(UIPanel panel, int index)
        {
            if (Panels.TryGetValue(panel, out var metaData))
            {
                var panelData = PanelStructs[metaData.Index];
                panelData.MaskIndex = index;
                PanelStructs[metaData.Index] = panelData;
            }
            SetBufferUpdateState(BufferEnum.Update);
        }

        public void UpdateTextureIndex(UIPanel panel)
        {
            if (Panels.TryGetValue(panel, out var metaData))
            {
                var panelData = PanelStructs[metaData.Index];
                panelData.TextureIndex = panel.TextureID;
                PanelStructs[metaData.Index] = panelData;
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
            foreach (var (panel, metaData) in Panels)
            {
                //Console.WriteLine(panel.GetName() + " has a visibility of: " + panel.Visible + " at: " + index + " and: " + i);
                if (panel.IsValid)
                {
                    var data = PanelStructs[i];
                    if (metaData.Index != data.ElementIndex)
                    {
                        data.ElementIndex = metaData.Index;
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
            foreach (var (panel, metaData) in Panels)
            {
                var panelStruct = PanelStructs[metaData.Index];
                panelStruct.Size = panel.Size;
                panelStruct.Slice = panel.Slice;
                panelStruct.Transform = panel.Transform.Xyz;
                PanelStructs[metaData.Index] = panelStruct;
            }
            _uiSSBO.Update(PanelStructs);
        }

        public void UpdateTransform(UIPanel panel)
        {
            if (!Panels.TryGetValue(panel, out var metaData))
                return;

            var panelStruct = PanelStructs[metaData.Index];
            panelStruct.Transform = panel.Transform.Xyz;
            PanelStructs[metaData.Index] = panelStruct;

            SetBufferUpdateState(BufferEnum.Update);
        }

        public void UpdateScale(UIPanel panel)
        {
            if (!Panels.TryGetValue(panel, out var metaData))
                return;

            var panelStruct = PanelStructs[metaData.Index];
            panelStruct.Size = panel.Size;
            panelStruct.Slice = panel.Slice;
            PanelStructs[metaData.Index] = panelStruct;

            SetBufferUpdateState(BufferEnum.Update);
        }

        public void UpdateColor(UIPanel panel)
        {
            if (!Panels.TryGetValue(panel, out var metaData))
                return;

            var panelStruct = PanelStructs[metaData.Index];
            panelStruct.Color = panel.Color;
            PanelStructs[metaData.Index] = panelStruct;

            _updateVisibility = true;
            if (_bufferUpdateState != BufferEnum.Recreate)
                SetBufferUpdateState(BufferEnum.Update);
        }

        public void UpdateBorderUI(UIPanel panel)
        {
            if (!Panels.TryGetValue(panel, out var metaData))
                return;

            StyleData[metaData.StyleIndex] = panel.BorderUI;

            _updateVisibility = true;
            if (_bufferUpdateState != BufferEnum.Recreate)
                SetBufferUpdateState(BufferEnum.Update);
        }

        public void UpdateBorderColor(UIPanel panel)
        {
            if (!Panels.TryGetValue(panel, out var metaData))
                return;

            StyleData[metaData.StyleIndex + 1] = panel.BorderColor;

            _updateVisibility = true;
            if (_bufferUpdateState != BufferEnum.Recreate)
                SetBufferUpdateState(BufferEnum.Update);
        }

        public void UpdateAnimationTranslation(UIPanel panel)
        {
            if (!Panels.TryGetValue(panel, out var metaData))
                return;

            var styleData = StyleData[metaData.StyleIndex + 2];
            styleData.Xy = panel.AnimationTranslation;
            StyleData[metaData.StyleIndex + 2] = styleData;

            SetBufferUpdateState(BufferEnum.Update);
        }

        public void UpdateAnimationScale(UIPanel panel)
        {
            if (!Panels.TryGetValue(panel, out var metaData))
                return;

            var styleData = StyleData[metaData.StyleIndex + 2];
            styleData.Z = panel.AnimationScale;
            StyleData[metaData.StyleIndex + 2] = styleData;

            SetBufferUpdateState(BufferEnum.Update);
        }

        public void UpdateAnimationRotation(UIPanel panel)
        {
            if (!Panels.TryGetValue(panel, out var metaData))
                return;

            var styleData = StyleData[metaData.StyleIndex + 2];
            styleData.W = panel.AnimationRotation;
            StyleData[metaData.StyleIndex + 2] = styleData;

            SetBufferUpdateState(BufferEnum.Update);
        }

        public void UpdateGraph(UIGraph graph)
        {
            if (!Panels.TryGetValue(graph, out var metaData))
                return;

            for (int i = 0; i < graph.Points.Length; i++)
            {
                int outerIndex = i >> 2;
                int innerIndex = i & 3;
                var styleData = StyleData[metaData.StyleIndex + 1 + outerIndex];
                styleData[innerIndex] = graph.Points[i];
                StyleData[metaData.StyleIndex + 1 + outerIndex] = styleData;
            }

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
                    _styleSSBO.Update(StyleData);
                    break;
                case BufferEnum.Recreate:
                    foreach (var panel in PanelStructsToBeRemoved)
                        Panels.Remove(panel);

                    foreach (var panel in PanelStructsToBeAdded)
                        Panels.TryAdd(panel, new());

                    ElementCount = 0;
                    PanelStructs = new NewUIPanelStruct[Panels.Count];
                    List<Vector4> StyleDatas = [];

                    foreach (var (panel, oldMetaData) in Panels)
                    {
                        var metaData = oldMetaData;
                        metaData.Index = ElementCount;
                        metaData.StyleIndex = StyleDatas.Count;
                        Panels[panel] = metaData;

                        int styleCount = 0;
                        if (panel is UIGraph uiGraph)
                        {
                            styleCount = 1;
                            StyleDatas.Add(new(uiGraph.PointCount, 1, 0, 0));
                            for (int i = 0; i < uiGraph.PointCount; i +=4 )
                                StyleDatas.Add(new Vector4(0));
                        }
                        else
                        {
                            styleCount = 2;
                            StyleDatas.Add(panel.BorderUI);
                            StyleDatas.Add(panel.BorderColor);
                            StyleDatas.Add(new Vector4(panel.AnimationTranslation.X, panel.AnimationTranslation.Y, panel.AnimationScale, panel.AnimationRotation));
                        }
                        
                        PanelStructs[ElementCount] = new NewUIPanelStruct
                        {
                            Size = panel.Size,
                            Slice = panel.Slice,
                            Color = panel.Color,
                            Transform = panel.Transform.Xyz,
                            ElementIndex = ElementCount,
                            TextureIndex = panel.TextureID,
                            MaskIndex = panel.MaskIndex,
                            StyleIndex = metaData.StyleIndex,
                            StyleInfo = styleCount
                        };  
                        
                        ElementCount++;
                    }

                    StyleData = [..StyleDatas];

                    PanelStructsToBeRemoved = [];
                    PanelStructsToBeAdded = [];

                    UpdateVisibility();

                    _uiSSBO.Renew(PanelStructs);
                    _styleSSBO.Renew(StyleData);
                    Descriptor.BindSSBO(_uiSSBO, 1);
                    Descriptor.BindSSBO(_styleSSBO, 2);
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

        public struct UIMetaData
        {
            public int Index;
            public int StyleIndex;
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

    public struct NewUIPanelStruct
    {
        public Vector2 Size;
        public Vector2 Slice; // 9 slice
        public Vector4 Color;
        public Vector3 Transform;
        public int ElementIndex; // points to itself or later element in the same buffer, used to keep same buffer even when elements in the middle are not visible
        public int TextureIndex;
        public int MaskIndex;
        public int StyleIndex;
        public int StyleInfo; // split into 8 sections of 4 bits, the first 4 bits is the amount of styles to loop over, so 7 max (which is fine) and the next sections are just flags to tell the vertex shader what the next style is and it will look in the buffer accordingly
    }
}