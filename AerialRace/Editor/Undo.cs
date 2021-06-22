using AerialRace.Debugging;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AerialRace.Editor
{
    public interface IAction
    {
        public void DoAction();
        public void UndoAction();
    }

    public interface IDoer
    {
        public void DoAction();
        public void UndoAction();
        public void RedoAction();
    }

    public interface IDoer<in T>
    {
        public void DoAction(T action);
        public void UndoAction(T action);
        public void RedoAction(T action);
    }

    public class UndoStack
    {
        public IAction[] Elements;
        public int Count;
        public int AvailableRedos;

        public UndoStack(int elements)
        {
            Elements = new IAction[elements];
            Count = 0;
            AvailableRedos = 0;
        }

        public void EnsureSize(int size)
        {
            if (Elements.Length < size)
            {
                Array.Resize(ref Elements, Math.Max(Elements.Length + Elements.Length / 2, size));
            }
        }

        public void PushAlreadyDone(IAction element)
        {
            EnsureSize(Count + 1);
            Elements[Count] = element;
            Count++;
            AvailableRedos = 0;
        }

        public void Do(IAction element)
        {
            element.DoAction();
            PushAlreadyDone(element);
        }

        public bool TryUndo()
        {
            if (Count - 1 < 0)
                return false;

            Count--;
            AvailableRedos++;
            Elements[Count].UndoAction();
            return true;
        }

        public bool TryRedo()
        {
            if (AvailableRedos <= 0)
                return false;

            AvailableRedos--;
            Count++;
            Elements[Count].DoAction();
            return true;
        }
    }

    static class Undo
    {
        public static UndoStack EditorUndoStack = new UndoStack(100);
    }

    struct Translate : IAction
    {
        public Transform Transform;
        public Vector3 StartPosition;
        public Vector3 EndPosition;

        public void DoAction()
        {
            Transform.LocalPosition = EndPosition;
        }

        public void UndoAction()
        {
            Transform.LocalPosition = StartPosition;
        }
    }

    struct Rotate : IAction
    {
        public Transform Transform;
        public Quaternion StartRotation;
        public Quaternion EndRotation;

        public void DoAction()
        {
            Transform.LocalRotation = EndRotation;
        }

        public void UndoAction()
        {
            Transform.LocalRotation = StartRotation;
        }
    }

    struct Scale : IAction
    {
        public Transform Transform;
        public Vector3 StartScale;
        public Vector3 EndScale;

        public void DoAction()
        {
            Transform.LocalScale = EndScale;
        }

        public void UndoAction()
        {
            Transform.LocalScale = StartScale;
        }
    }

    struct PhysTranslate : IAction
    {
        public StaticSetpiece Setpiece;
        public Vector3 StartPosition;
        public Vector3 EndPosition;

        public void DoAction()
        {
            var pos = EndPosition + Setpiece.StaticCollider.Shape.Center;
            Setpiece.StaticCollider.Static.Pose.Position = pos.AsNumerics();
        }

        public void UndoAction()
        {
            var pos = StartPosition + Setpiece.StaticCollider.Shape.Center;
            Setpiece.StaticCollider.Static.Pose.Position = pos.AsNumerics();
        }
    }
}
