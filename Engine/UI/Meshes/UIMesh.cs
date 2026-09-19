
using System.Runtime.InteropServices;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Parse;
using PBG.UI;

namespace PBG.Rendering.Meshes
{
    public class UIMesh
    {
        public uint ElementCount = 0;
        public int VisibleElementCount = 0;

        public HashSet<UIPanel> PanelStructsToBeRemoved = [];
        public HashSet<UIPanel> PanelStructsToBeAdded = [];
        public NewUIPanelStruct[] PanelStructs = [];
        public Vector4[] StyleData = [];
        public Dictionary<UIPanel, UIMetaData> Panels = [];
        public List<UIPanel> PanelList = [];

        private uint _panelMinIndex = uint.MaxValue;
        private uint _panelMaxIndex = uint.MinValue;

        private uint _styleMinIndex = uint.MaxValue;
        private uint _styleMaxIndex = uint.MinValue;

        private SSBO<NewUIPanelStruct> _uiSSBO = new([], true);
        private SSBO<Vector4> _styleSSBO = new([], true);

        private bool _updateVisibility = false;
        private bool _updateDepth = false;
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
                UpdatePanelData(metaData.Index);
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
                UpdatePanelData(metaData.Index);
            }
            SetBufferUpdateState(BufferEnum.Update);
        }

        public void UpdateVisible(UIPanel panel)
        {
            if (Panels.TryGetValue(panel, out var metaData))
            {
                var panelData = PanelStructs[metaData.Index];
                panelData.SetVisible(panel.Visible);
                PanelStructs[metaData.Index] = panelData;
                UpdatePanelData(metaData.Index);
            }
            SetBufferUpdateState(BufferEnum.Update);
            _updateVisibility = true;
        }

        public void QueueUpdateVisibility()
        {
            _updateVisibility = true;
            if (_bufferUpdateState != BufferEnum.Recreate)
                SetBufferUpdateState(BufferEnum.Update);
        }

        public void QueueUpdateDepth()
        {
            _updateDepth = true;
            if (_bufferUpdateState != BufferEnum.Recreate)
                SetBufferUpdateState(BufferEnum.Update);
        }

        public void UpdateVisibility()
        {
            int count = 0;
            VisibleElementCount = 0;
            for (int i = 0; i < PanelStructs.Length; i++)
            {
                var panelStruct = PanelStructs[i];
                if (panelStruct.IsVisible() && !panelStruct.IsInvisible(this))
                {
                    var data = PanelStructs[count];
                    if (i != data.GetElementIndex())
                    {
                        data.SetElementIndex((uint)i);
                        PanelStructs[count] = data;
                    }
                    count++;
                    VisibleElementCount++;
                }
            }

            UpdatePanelData(0);
            UpdatePanelData(PanelStructs.Length);

            _updateVisibility = false;
        }

        public void UpdateDepth()
        {
            List<UIPanel> sorted = [.. Panels.Keys];

            sorted.Sort((a, b) =>
            {
                bool at = a.HasTransparency();
                bool bt = b.HasTransparency();

                if (at != bt)
                    return at.CompareTo(bt); // opaque first

                return at
                    ? a.GetTotalDepth().CompareTo(b.GetTotalDepth())   // transparent front-to-back
                    : b.GetTotalDepth().CompareTo(a.GetTotalDepth());  // opaque back-to-front
            });

            NewUIPanelStruct[] newPanels = new NewUIPanelStruct[PanelStructs.Length];

            for (int i = 0; i < sorted.Count; i++)
            {
                var panel = sorted[i];
                var meta = Panels[panel];

                newPanels[i] = PanelStructs[meta.Index];

                meta.Index = (uint)i;
                Panels[panel] = meta;
            }

            PanelStructs = newPanels;

            UpdatePanelData(0);
            UpdatePanelData(PanelStructs.Length);

            _updateDepth = false;
            _updateVisibility = true;
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
                UpdatePanelData(metaData.Index);
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
            UpdatePanelData(metaData.Index);

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
            UpdatePanelData(metaData.Index);

            SetBufferUpdateState(BufferEnum.Update);
        }

        public void UpdateColor(UIPanel panel)
        {
            if (!Panels.TryGetValue(panel, out var metaData))
                return;

            var panelStruct = PanelStructs[metaData.Index];
            if (panelStruct.HasTransparency(this))
                _updateDepth = true;
                
            panelStruct.Color = panel.Color;
            PanelStructs[metaData.Index] = panelStruct;
            UpdatePanelData(metaData.Index);

            _updateVisibility = true;
            if (_bufferUpdateState != BufferEnum.Recreate)
                SetBufferUpdateState(BufferEnum.Update);
        }

        public void UpdateBorderUI(UIPanel panel)
        {
            if (!Panels.TryGetValue(panel, out var metaData))
                return;

            StyleData[metaData.StyleIndex] = panel.BorderUI;
            UpdateStyleData(metaData.StyleIndex);

            _updateVisibility = true;
            if (_bufferUpdateState != BufferEnum.Recreate)
                SetBufferUpdateState(BufferEnum.Update);
        }

        public void UpdateBorderColor(UIPanel panel)
        {
            if (!Panels.TryGetValue(panel, out var metaData))
                return;

            var panelStruct = PanelStructs[metaData.Index];
            if (panelStruct.HasTransparency(this))
                _updateDepth = true;

            if (panelStruct.StyleInfo == 2)
            {
                StyleData[metaData.StyleIndex + 1] = panel.BorderColor;
                UpdateStyleData(metaData.StyleIndex + 1);
            }

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
            UpdateStyleData(metaData.StyleIndex + 2);

            SetBufferUpdateState(BufferEnum.Update);
        }

        public void UpdateAnimationScale(UIPanel panel)
        {
            if (!Panels.TryGetValue(panel, out var metaData))
                return;

            var styleData = StyleData[metaData.StyleIndex + 2];
            styleData.Z = panel.AnimationScale;
            StyleData[metaData.StyleIndex + 2] = styleData;
            UpdateStyleData(metaData.StyleIndex + 2);

            SetBufferUpdateState(BufferEnum.Update);
        }

        public void UpdateAnimationRotation(UIPanel panel)
        {
            if (!Panels.TryGetValue(panel, out var metaData))
                return;

            var styleData = StyleData[metaData.StyleIndex + 2];
            styleData.W = panel.AnimationRotation;
            StyleData[metaData.StyleIndex + 2] = styleData;
            UpdateStyleData(metaData.StyleIndex + 2);

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
                int styleIndex = metaData.StyleIndex + 1 + outerIndex;
                var styleData = StyleData[styleIndex];
                styleData[innerIndex] = graph.Points[i];
                StyleData[styleIndex] = styleData;
                UpdateStyleData(styleIndex);
            }

            SetBufferUpdateState(BufferEnum.Update);
        }

        public void UpdatePanelData(int index) => UpdatePanelData((uint)index);
        public void UpdatePanelData(uint index)
        {
            _panelMinIndex.MinSet(index);
            _panelMaxIndex.MaxSet(index);
        }

        public void UpdateStyleData(int index) => UpdateStyleData((uint)index);
        public void UpdateStyleData(uint index)
        {
            _styleMinIndex.MinSet(index);
            _styleMaxIndex.MaxSet(index);
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

        private void ResetPanelData()
        {
            _panelMinIndex = uint.MaxValue;
            _panelMaxIndex = uint.MinValue;
        }

        private void ResetStyleData()
        {
            _styleMinIndex = uint.MaxValue;
            _styleMaxIndex = uint.MinValue;
        }

        private void UpdatePanelDataSSBO()
        {
            if (_panelMinIndex >= _panelMaxIndex)
            {
                ResetPanelData();
                return;
            }

            uint size = _panelMaxIndex - _panelMinIndex;

            uint offsetInBytes = _panelMinIndex * NewUIPanelStruct.ByteSize;
            uint sizeInBytes = size * NewUIPanelStruct.ByteSize;

            _uiSSBO.UpdateSlice(PanelStructs, offsetInBytes, sizeInBytes);

            ResetPanelData();
        }

        private void UpdateStyleDataSSBO()
        {
            if (_styleMinIndex >= _styleMaxIndex)
            {
                ResetStyleData();
                return;
            }

            uint size = _styleMaxIndex - _styleMinIndex;

            uint offsetInBytes = _styleMinIndex * Vector4.ByteSize;
            uint sizeInBytes = size * Vector4.ByteSize;

            _styleSSBO.UpdateSlice(StyleData, offsetInBytes, sizeInBytes);

            ResetStyleData();
        }

        private static uint SetStyle(ref uint styleInfo, int index, uint value)
        {
            if (index < 0 || index >= 10)
                return styleInfo;

            styleInfo &= ~(7u << (index*3));
            styleInfo |= (value & 7) << (index*3);
            return styleInfo;
        }

        private void UpdateBuffers()
        {
            switch (_bufferUpdateState)
            {
                case BufferEnum.Update:
                    if (_updateDepth)
                        UpdateDepth();

                    if (_updateVisibility)
                        UpdateVisibility();
                    
                    UpdatePanelDataSSBO();
                    UpdateStyleDataSSBO();
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

                        uint styleInfo = 0;
                        if (panel is UIGraph uiGraph)
                        {
                            SetStyle(ref styleInfo, 0, 1);
                            StyleDatas.Add(new(uiGraph.PointCount, 1, 0, 0));
                            for (int i = 0; i < uiGraph.PointCount; i +=4 )
                                StyleDatas.Add(new Vector4(0));
                        }
                        else
                        {
                            SetStyle(ref styleInfo, 0, 2);
                            StyleDatas.Add(panel.BorderUI);
                            StyleDatas.Add(panel.BorderColor);
                            StyleDatas.Add(new Vector4(panel.AnimationTranslation.X, panel.AnimationTranslation.Y, panel.AnimationScale, panel.AnimationRotation));
                        }

                        SetStyle(ref styleInfo, 9, (uint)panel.panelRotation);

                        
                        var panelStruct = new NewUIPanelStruct
                        {
                            Size = panel.Size,
                            Slice = panel.Slice,
                            Color = panel.Color,
                            Transform = panel.Transform.Xyz,
                            TextureIndex = panel.TextureID,
                            MaskIndex = panel.MaskIndex,
                            StyleIndex = metaData.StyleIndex,
                            StyleInfo = styleInfo
                        };  

                        panelStruct.SetElementIndex(ElementCount);
                        panelStruct.SetVisible(panel.IsValid);

                        PanelStructs[ElementCount] = panelStruct;
                        
                        ElementCount++;
                    }

                    StyleData = [..StyleDatas];

                    PanelStructsToBeRemoved = [];
                    PanelStructsToBeAdded = [];

                    UpdateDepth();
                    UpdateVisibility();

                    ResetPanelData();
                    ResetStyleData();

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
            //Console.WriteLine(VisibleElementCount);
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
            public uint Index;
            public int StyleIndex;
        }
    }

    public struct NewUIPanelStruct
    {
        public static readonly uint ByteSize = (uint)Marshal.SizeOf<NewUIPanelStruct>();

        public Vector2 Size;
        public Vector2 Slice; // 9 slice
        public Vector4 Color;
        public Vector3 Transform;
        public uint ElementIndex; // points to itself or later element in the same buffer, used to keep same buffer even when elements in the middle are not visible
        public int TextureIndex;
        public int MaskIndex;
        public int StyleIndex;
        public uint StyleInfo; // split into 8 sections of 4 bits, the first 4 bits is the amount of styles to loop over, so 7 max (which is fine) and the next sections are just flags to tell the vertex shader what the next style is and it will look in the buffer accordingly

        public void SetVisible(bool visible)
        {
            ElementIndex = (visible ? 0x80000000 : 0) | (ElementIndex & 0x7FFFFFFF);
        }

        public bool IsVisible()
        {
            return (ElementIndex & 0x80000000) != 0;
        }

        public void SetElementIndex(uint index)
        {
            ElementIndex = (index & 0x7FFFFFFF) | (ElementIndex & 0x80000000);
        }

        public uint GetElementIndex() => ElementIndex & 0x7FFFFFFF;

        public readonly bool HasTransparency(UIMesh mesh)
        {
            bool transparency = false;
            if (StyleInfo == 2)
            {
                var borderColor = mesh.StyleData[StyleIndex+1];
                transparency = borderColor.W < 1.0f;
            }

            transparency |= Color.W < 1.0f;

            return transparency;
        }

        public readonly bool IsInvisible(UIMesh mesh)
        {
            bool transparency = false;
            if (StyleInfo == 2)
            {
                var borderColor = mesh.StyleData[StyleIndex+1];
                transparency = borderColor.W <= 0.0f;
            }

            transparency &= Color.W <= 0.0f;

            return transparency;
        }


        public override string ToString()
        {
            return $"Size: {Size} Slice: {Slice} Color: {Color} Transform: {Transform} Visible: {IsVisible()} ElementIndex: {GetElementIndex()} TextureIndex: {TextureIndex} MaskIndex: {MaskIndex} StyleIndex: {StyleIndex} StyleInfo {StyleInfo}";
        }
    }
}