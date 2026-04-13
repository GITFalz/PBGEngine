namespace PBG.UI
{
    public abstract partial class UIElementBase
    {
        public class UIQueueAction(UIElementBase element)
        {
            public UIElementBase Element = element;
            public UIQueueEntry Actions;
            public bool QueuedVisibility = false;

            public void Execute()
            {
                if (Actions.HasFlag(UIQueueEntry.Align))
                    Element.Align();

                if (Actions.HasFlag(UIQueueEntry.Transform))
                    Element.UpdateTransform();

                if (Actions.HasFlag(UIQueueEntry.Scale))
                    Element.UpdateScale();

                if (Actions.HasFlag(UIQueueEntry.Visibility))
                    Element.SetVisible(QueuedVisibility);

                if (Actions.HasFlag(UIQueueEntry.DisableAnimation))
                    Element.IsAnimating = false;
            }
        }
    }
}