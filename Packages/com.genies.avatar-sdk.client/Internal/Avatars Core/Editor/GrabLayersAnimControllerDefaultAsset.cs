using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace Genies.Avatars.Sdk.Editor
{
    /// <summary>
    /// Asset that defines default grab pose animation layers and parameters to be applied to avatar animator controllers.
    /// This asset copies grab-related animation layers, state machines, and parameters from a reference controller.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "GrabLayersAnimControllerDefaultAsset.asset", menuName = "Genies/Anim Controller Defaults/Grab layers data asset")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class GrabLayersAnimControllerDefaultAsset : AnimControllerDefaultAsset
#else
    public class GrabLayersAnimControllerDefaultAsset : AnimControllerDefaultAsset
#endif
    {
        /// <summary>
        /// Applies grab pose animation layers and parameters from the reference controller to the target controller.
        /// Copies all layers except the base layer, along with their state machines, transitions, and required parameters.
        /// </summary>
        /// <param name="target">The AnimatorController to apply grab layers and parameters to.</param>
        public override void ApplyToTargetController(AnimatorController target)
        {
            // Layer step
            HashSet<string> existingLayerNames = new HashSet<string>();
            foreach (var layer in target.layers)
            {
                existingLayerNames.Add(layer.name);
            }

            for (int i=0; i< RefController.layers.Length; i++)
            {
                // Output from refController.layers is a shallow copy, so we have to do extra work to get an unconnected deep copy.
                // Reference: (https://docs.unity3d.com/ScriptReference/Animations.AnimatorController-layers.html)
                var layer = RefController.layers[i];

                // Skip the base layer, we only want the extra layers
                if (layer.name == "Base Layer")
                {
                    continue;
                }

                // Skip if the layer exists in target controller already
                if (existingLayerNames.Contains(layer.name))
                {
                    continue;
                }

                target.AddLayer(layer.name);
                int targetIndex = target.layers.Length - 1;
                var targetLayer = target.layers[targetIndex];

                ConfigureLayer(RefController, i, target, targetIndex); // Clear the newly added layer
                CopyStateMachine(layer.stateMachine, targetLayer.stateMachine, target, targetIndex);
                CopyTransitions(layer.stateMachine, targetLayer.stateMachine, target, targetIndex);
            }

            // Parameter step
            HashSet<string> targetParamNames = new HashSet<string>();
            foreach (var param in target.parameters)
            {
                targetParamNames.Add(param.name);
            }

            // Get ref parameters. Note that these are a copy and not a direct reference (https://docs.unity3d.com/ScriptReference/Animations.AnimatorController-parameters.html)
            AnimatorControllerParameter[] refParams = RefController.parameters;

            foreach (var param in refParams)
            {
                // Skip if the param exists in target controller already
                if (targetParamNames.Contains(param.name))
                {
                    continue;
                }

                target.AddParameter(param.name, param.type);
            }

            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
        }

        private void ConfigureLayer(AnimatorController source, int sourceIndex, AnimatorController target, int targetIndex)
        {
            // Clear transitions
            foreach (var state in target.layers[targetIndex].stateMachine.states)
            {
                foreach (var transition in state.state.transitions)
                {
                    state.state.RemoveTransition(transition);
                }
            }

            // Clear states
            foreach (var state in target.layers[targetIndex].stateMachine.states)
            {
                target.layers[targetIndex].stateMachine.RemoveState(state.state);
            }

            // Copy values from source
            var layersCopy = target.layers;

            layersCopy[targetIndex].avatarMask = source.layers[sourceIndex].avatarMask; // This references a project asset, and we want to preserve that link
            layersCopy[targetIndex].blendingMode = source.layers[sourceIndex].blendingMode;
            layersCopy[targetIndex].defaultWeight = source.layers[sourceIndex].defaultWeight;
            layersCopy[targetIndex].iKPass = source.layers[sourceIndex].iKPass;
            layersCopy[targetIndex].name = source.layers[sourceIndex].name;
            layersCopy[targetIndex].syncedLayerAffectsTiming = source.layers[sourceIndex].syncedLayerAffectsTiming;
            layersCopy[targetIndex].syncedLayerIndex = source.layers[sourceIndex].syncedLayerIndex;

            target.layers = layersCopy;
        }

        private void CopyStateMachine(AnimatorStateMachine sourceMachine, AnimatorStateMachine targetMachine, AnimatorController targetController, int targetLayerIndex)
        {
            // Copy states
            foreach (var state in sourceMachine.states)
            {
                if (state.state.motion.GetType() == typeof(AnimationClip))
                {
                    var newState = targetMachine.AddState(state.state.name, state.position);
                    newState.motion = state.state.motion;
                }

                else if (state.state.motion.GetType() == typeof(BlendTree))
                {
                    BlendTree oldBlendTree = state.state.motion as BlendTree;
                    var newState = targetController.CreateBlendTreeInController(oldBlendTree.name, out BlendTree newBlendTree, targetLayerIndex);

                    newBlendTree.blendParameter = oldBlendTree.blendParameter;
                    newBlendTree.blendParameterY = oldBlendTree.blendParameterY;
                    newBlendTree.blendType = oldBlendTree.blendType;
                    newBlendTree.minThreshold = oldBlendTree.minThreshold;
                    newBlendTree.maxThreshold = oldBlendTree.maxThreshold;
                    newBlendTree.useAutomaticThresholds = oldBlendTree.useAutomaticThresholds;

                    foreach (var child in oldBlendTree.children)
                    {
                        newBlendTree.AddChild(child.motion, child.threshold);
                    }

                    for (int i = 0; i < oldBlendTree.children.Length; i++)
                    {
                        newBlendTree.children[i].timeScale = oldBlendTree.children[i].timeScale;
                        newBlendTree.children[i].position = oldBlendTree.children[i].position;
                        newBlendTree.children[i].threshold = oldBlendTree.children[i].threshold;
                        newBlendTree.children[i].directBlendParameter = oldBlendTree.children[i].directBlendParameter;
                    }

                    newState.motion = newBlendTree;
                }
            }

            // Copy substate machines
            for (int i = 0; i < sourceMachine.stateMachines.Length; i++)
            {
                // There is a method in the API for state machines called MakeUniqueStateMachineName, and the other script didn't assign the name directly... I wonder whether duplicate names like this are allowed.
                targetMachine.AddStateMachine(sourceMachine.stateMachines[i].stateMachine.name, sourceMachine.stateMachines[i].position);
                CopyStateMachine(sourceMachine.stateMachines[i].stateMachine, targetMachine.stateMachines[i].stateMachine, targetController, targetLayerIndex); // yayyyy recursion
            }

            // Copy substate transitions
            for (int i = 0; i < sourceMachine.stateMachines.Length; i++)
            {
                CopyTransitions(sourceMachine.stateMachines[i].stateMachine, targetMachine.stateMachines[i].stateMachine, targetController, targetLayerIndex);
            }
        }

        private void CopyTransitions(AnimatorStateMachine sourceMachine, AnimatorStateMachine targetMachine, AnimatorController targetController, int targetLayerIndex)
        {
            // Any State transitions
            foreach (var transition in sourceMachine.anyStateTransitions)
            {
                AnimatorStateTransition newTransition = null;

                // Destination is a state
                if (transition.destinationState != null)
                {
                    foreach (var state in targetMachine.states)
                    {
                        if (state.state.name == transition.destinationState.name)
                        {
                            newTransition = targetMachine.AddAnyStateTransition(state.state);
                            break;
                        }

                        Debug.LogWarning($"No matching state found for state {transition.destinationState.name}");
                    }
                }
                // Destination is a state machine
                else if (transition.destinationStateMachine != null)
                {
                    foreach (var subMachine in targetMachine.stateMachines)
                    {
                        if (subMachine.stateMachine.name == transition.destinationStateMachine.name)
                        {
                            newTransition = targetMachine.AddAnyStateTransition(subMachine.stateMachine);
                            break;
                        }

                        Debug.LogWarning($"No matching state machine found for state machine {transition.destinationStateMachine.name}");
                    }
                }

                if (newTransition == null)
                {
                    continue;
                }

                newTransition.conditions = transition.conditions;
                newTransition.canTransitionToSelf = transition.canTransitionToSelf;
                newTransition.hasExitTime = transition.hasExitTime;
                newTransition.hasFixedDuration = transition.hasFixedDuration;
                newTransition.exitTime = transition.exitTime;
                newTransition.duration = transition.duration;
                newTransition.interruptionSource = transition.interruptionSource;
            }

            // Entry transitions
            foreach (var transition in sourceMachine.entryTransitions)
            {
                AnimatorTransition newEntryTransition = null;

                // Destination is a state
                if (transition.destinationState != null)
                {
                    foreach (var state in targetMachine.states)
                    {
                        if (state.state.name == transition.destinationState.name)
                        {
                            newEntryTransition = targetMachine.AddEntryTransition(state.state);
                            break;
                        }

                        Debug.LogWarning($"No matching state found for state {transition.destinationState.name}");
                    }
                }
                // Destination is a state machine
                else if (transition.destinationStateMachine != null)
                {
                    foreach (var subMachine in targetMachine.stateMachines)
                    {
                        if (subMachine.stateMachine.name == transition.destinationStateMachine.name)
                        {
                            newEntryTransition = targetMachine.AddEntryTransition(subMachine.stateMachine);
                            break;
                        }

                        Debug.LogWarning($"No matching state machine found for state machine {transition.destinationStateMachine.name}");
                    }
                }

                if (newEntryTransition == null)
                {
                    continue;
                }

                newEntryTransition.conditions = transition.conditions;
            }

            // Individual state transitions
            for (int i = 0; i<sourceMachine.states.Length; i++)
            {
                var state = sourceMachine.states[i];
                var targetState = targetMachine.states[i];

                foreach (var transition in state.state.transitions)
                {
                    AnimatorStateTransition newStateTransition = null;

                    // Destination is a state
                    if (transition.destinationState != null)
                    {
                        List<ChildAnimatorState> statesToCheck = new List<ChildAnimatorState>();

                        foreach (var otherState in targetMachine.states) // Other states in the target machine
                        {
                            statesToCheck.Add(otherState);
                        }

                        foreach (var targetLayerState in targetController.layers[targetLayerIndex].stateMachine.states) // States in the overall layer's state machine
                        {
                            statesToCheck.Add(targetLayerState);
                        }

                        foreach (var targetLayerSubMachine in targetController.layers[targetLayerIndex].stateMachine.stateMachines) // States in the overall layer's sub-state machines
                        {
                            foreach (var subMachineState in targetLayerSubMachine.stateMachine.states)
                            {
                                statesToCheck.Add(subMachineState);
                            }
                        }

                        foreach (var stateToCheck in statesToCheck)
                        {
                            if (stateToCheck.state.name == transition.destinationState.name)
                            {
                                newStateTransition = targetState.state.AddTransition(stateToCheck.state);
                                break;
                            }

                            Debug.LogWarning($"No matching state found for state {transition.destinationState.name}");
                        }

                    }
                    // Destination is a state machine
                    else if (transition.destinationStateMachine != null)
                    {
                        foreach (var subMachine in targetMachine.stateMachines)
                        {
                            if (subMachine.stateMachine.name == transition.destinationStateMachine.name)
                            {
                                newStateTransition = targetState.state.AddTransition(subMachine.stateMachine);
                                break;
                            }

                            Debug.LogWarning($"No matching state machine found for state machine {transition.destinationStateMachine.name}");
                        }
                    }

                    if (newStateTransition == null)
                    {
                        continue;
                    }

                    if (transition.isExit)
                    {
                        newStateTransition.isExit = transition.isExit;
                    }

                    newStateTransition.hasExitTime = transition.hasExitTime;
                    newStateTransition.hasFixedDuration = transition.hasFixedDuration;
                    newStateTransition.exitTime = transition.exitTime;
                    newStateTransition.duration = transition.duration;
                    newStateTransition.canTransitionToSelf = transition.canTransitionToSelf;
                    newStateTransition.conditions = transition.conditions;
                    newStateTransition.interruptionSource = transition.interruptionSource;
                    newStateTransition.orderedInterruption = transition.orderedInterruption;

                }
            }
        }
    }
}
