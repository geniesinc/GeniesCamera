using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using Genies.Utilities.UndoRedo;

namespace Genies.Customization.Framework.Navigation
{
    /// <summary>
    /// Controls a <see cref="INavigationNode"/> stack. Each <see cref="NavigationStack"/> has its own
    /// breadcrumbs and undo/redo commands.
    /// </summary>
    public class NavigationStack
    {
        private readonly Stack<INavigationNode> _stack = new Stack<INavigationNode>();
        private readonly IUndoRedoService<ICommand> _undoRedoService = new UndoRedoService<ICommand>(200);
        private readonly Dictionary<INavigationNode, INavigationNode> _nodeToLastSelected = new Dictionary<INavigationNode, INavigationNode>();
        private readonly List<string> _breadCrumbs = new List<string>();
        private UniTaskCompletionSource _undoRedoCurrentTaskCompletionSource;
        private INavigationNode _rootNode;

        public IReadOnlyCollection<string> GetBreadCrumbs()
        {
            return _breadCrumbs;
        }

        /// <summary>
        /// Clears the current undo redo history
        /// </summary>
        public void ClearUndoRedoService()
        {
            _undoRedoService.ClearHistory();
        }

        public void Push(INavigationNode node)
        {
            //Always set the first node in the stack to be the root.
            if (_rootNode == null)
            {
                if (node.IsStackable)
                {
                    _breadCrumbs.Add(node.Controller.BreadcrumbName);
                }

                _rootNode = node;
                return;
            }

            //Don't push root node.
            if (node == _rootNode)
            {
                return;
            }

            //Can't push non stackable nodes.
            if (!node.IsStackable)
            {
                //If we have a current node, see if we should track this node as a child.
                var current = CurrentNode();
                if (current == null)
                {
                    return;
                }

                //Only keep track of non stackable children.
                if (!current.Children.Contains(node))
                {
                    return;
                }

                if (_nodeToLastSelected.TryGetValue(current, out _))
                {
                    _nodeToLastSelected[current] = node;
                }
                else
                {
                    _nodeToLastSelected.Add(current, node);
                }

                return;
            }

            if (_stack.Count == 0 || !Equals(CurrentNode(), node))
            {
                _stack.Push(node);
                _breadCrumbs.Add(node.Controller.BreadcrumbName);
            }
        }

        public void RegisterCommand(ICommand command)
        {
            _undoRedoService.RegisterUndo(command);
        }

        public bool HasUndo()
        {
            return _undoRedoService.HasUndo();
        }

        public bool HasRedo()
        {
            return _undoRedoService.HasRedo();
        }

        private async UniTask AwaitUndoRedoAsync()
        {
            while (_undoRedoCurrentTaskCompletionSource != null)
            {
                await _undoRedoCurrentTaskCompletionSource.Task;
            }
        }

        public async UniTask UndoCommand()
        {
            await AwaitUndoRedoAsync();
            var command = _undoRedoService.Undo();

            if (command != null)
            {
                _undoRedoCurrentTaskCompletionSource = new UniTaskCompletionSource();
                await command.UndoAsync();
                _undoRedoCurrentTaskCompletionSource = null;
            }
        }

        public async UniTask RedoCommand()
        {
            await AwaitUndoRedoAsync();
            var command = _undoRedoService.Redo();

            if (command != null)
            {
                _undoRedoCurrentTaskCompletionSource = new UniTaskCompletionSource();
                await command.ExecuteAsync();
                _undoRedoCurrentTaskCompletionSource = null;
            }
        }

        public bool IsEmpty()
        {
            return _stack.Count == 0 && _rootNode == null;
        }

        private void Pop()
        {
            var node = _stack.Pop();
            _breadCrumbs.RemoveAt(_breadCrumbs.Count - 1);

            if (_nodeToLastSelected.ContainsKey(node))
            {
                _nodeToLastSelected.Remove(node);
            }
        }

        public INavigationNode CurrentRootNode()
        {
            return _rootNode;
        }

        public INavigationNode CurrentNode()
        {
            if (_stack.Count == 0)
            {
                return _rootNode;
            }

            return _stack.Peek();
        }

        public INavigationNode GetLastNonStackableChild(INavigationNode forNode)
        {
            if (_nodeToLastSelected.TryGetValue(forNode, out var selection))
            {
                return selection;
            }

            return null;
        }

        public void RemoveLastNonStackableChild(INavigationNode forNode)
        {
            if (_nodeToLastSelected.ContainsKey(forNode))
            {
                _nodeToLastSelected.Remove(forNode);
            }
        }

        public int GetBreadCrumbCount()
        {
            return _breadCrumbs.Count;
        }

        public async UniTask SaveOrExitAsync(bool isDiscard, bool isCreate)
        {
            //Check child editors first
            if (IsEmpty())
            {
                return;
            }

            bool GetCondition(ICustomizationController editor) => isDiscard ? editor.HasDiscardAction() : (isCreate ? editor.HasCreateAction() : editor.HasSaveAction());
            UniTask<bool> GetTask(ICustomizationController editor) => isDiscard ? editor.OnDiscardAsync() : (isCreate ? editor.OnCreateAsync() : editor.OnSaveAsync());
            int  nodesToPop        = 0;
            bool stackDidInterrupt = false;
            bool shouldPop         = false;

            int counter = 0;
            while (counter < _stack.Count)
            {
                var node = _stack.Peek();

                if (node == null || node.Controller == null)
                {
                    //Bad node
                    Pop();
                    CrashReporter.Log($"NavigationStack's SaveOrExitAsync encountered a null or null config node with isDiscard {isDiscard}, " +
                        $"isCreate {isCreate}, and at counter {counter}. Skipping it.", LogSeverity.Error);
                    continue;
                }

                var condition = GetCondition(node.Controller);

                //Node interrupts discard
                if (condition)
                {
                    var didSucceed = await GetTask(node.Controller);

                    //Made decision.
                    if (didSucceed)
                    {
                        //editor discarded successfully.
                        shouldPop = true;
                        nodesToPop++;
                    }

                    stackDidInterrupt = true;
                    break;
                }

                //Node can be discarded
                nodesToPop++;
                counter++;
            }

            //Run save or discard on root
            if (!stackDidInterrupt)
            {
                var condition = GetCondition(_rootNode.Controller);

                //Node interrupts discard
                if (condition)
                {
                    var didSucceed = await GetTask(_rootNode.Controller);

                    //Made decision.
                    if (didSucceed)
                    {
                        //Invalidate the stack
                        shouldPop = true;
                        nodesToPop++;
                    }

                    stackDidInterrupt = true;
                }
                else
                {
                    nodesToPop++;
                }
            }

            if (shouldPop || !stackDidInterrupt)
            {
                for (int i = 0; i < nodesToPop; i++)
                {
                    if (_stack.Count == 0)
                    {
                        _rootNode = null;
                    }
                    else
                    {
                        Pop();
                    }
                }
            }
        }

        public bool CanGoBack()
        {
            if (_rootNode == null)
            {
                return false;
            }

            //Non stackable root nodes are a special case. It means we can't go back to them and we only
            //consider the back logic if there's more than 1 node in the child stack
            return _rootNode.IsStackable ? _stack.Count > 0 : _stack.Count > 1;
        }

        public bool GoBack(int count = 1)
        {
            if (count <= 0)
            {
                return false;
            }

            if (!CanGoBack())
            {
                return false;
            }

            for (int i = 0; i < count; i++)
            {
                if (!CanGoBack())
                {
                    break;
                }

                Pop();
            }

            return true;
        }
    }
}
