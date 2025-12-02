# Customizer Framework

## What is it?

The customizer is a UI framework for customizing any GameObject. This means it has no dependencies on what you're editing, it can be an avatar, a background, an animation etc... 
the driver of the whole framework is a node graph, the reason this framework uses a node graph is for it to be compositional and allows us to customize the navigation without 
having to reimplement the logic. 

## Why?

Originally when we started designing the new editing UX for looks, it was planned to be mostly hard coded but product had other ideas! they wanted 2 different ways of navigating 
the editing experience. One for `MyGenie` and one for `MyLook` and both had very different navigations. So the idea to create a framework that 
is extensible and allows us to build compositional customization controllers meant we can scale this any way we'd like and adapt to new product needs in the future.

## Capabilities?

- Navigate through different controllers using a navigation graph
- Ability to add any number of navigation nodes and customize the navigation by changing the navigation graph object
- Ability to create customization controllers by composition. Each controller is attached to a navigation node.
- Transition animations between nodes out of the box.
- A generic scrolling item picker solution (used to populate a list of items that can be selected)
- A generic scrolling gallery item picker solution (same as above but a different layout)
- A navigation bar which is used to navigate the different nodes in the graph
- An action bar that allows the user to save, exit, undo, redo

## Layout?

The customizer has 6 core views

- The action bar (top bar for users to save, exit, undo, redo, etc...)
- The nav bar (bottom bar for navigating through the customizer nodes)
- The preview area (accordion like area, it masks anything inside of it, used to show the entity or GameObject being edited)
- The customization editor area (used to show the customization controls for the current node)
- The breadcrumbs area (used to show navigation breadcrumbs for the user to go back)
- The popups/overlays area (full screen area that is used for instantiating popups dynamically)

## What are the components?

### CustomizerController

The main controller of the whole customizer. Handles communications between the different components of the customizer and handles navigation requests.

### CustomizationController

- `ICustomizationController`
  - The actual class that handles the customization of the current node we're on.
  - The `CustomizerController` communicates action bar requests to these controllers for them to run their own custom logic.
  - Configures the customization's view and controller
- `CustomizerViewConfig`
  - Configures the customizer's views
    - Background colors
    - Which views to show (NavBar, ActionBar, Customization Editor, etc...)
    - How the node looks in the navigation bar
    - The breadcrumb name


### NavigationStack

When we are navigating through nodes we need to keep track of where we are to know how to go back, that's where the stack comes in. The stack also tracks 
any changes made in any of its nodes by providing an undo/redo service. New stacks are used if the node we navigate to is marked as a root node.

### NavigationGraph

A node graph that is built on top of [xNode](https://github.com/Siccity/xNode) which is a generic node graph solution built using IMGUI. The reason we went with [xNode](https://github.com/Siccity/xNode) is mainly due to
it being generic, makes no assumptions on how you use the graph and is built using IMGUI which means its stable and backwards compatible.

The NavigationGraph has 3 main components that are used for driving the customizer

#### INavigationNode

An interface abstraction, defines 3 ways of navigation to next nodes

- A list of child nodes (nodes that can be navigated to from the navigation bar)
- An EditItem node, a special node that can only be navigated to if the item you're currently on can be customized (Example UGCW, styles, patterns etc...)
- A CreateItem node, a special node that can only be navigated to if the current controller allows creating new items. (Example UGCW, styles, patterns)

Every navigation node also references a `CustomizationConfig`

#### NavigationRootNode

Every graph has this node, its created automatically when you create a new graph. The main difference is it takes no inputs.

#### NavigationNode

Similar to the NavigationRootNode but allows you to have inputs. The navigation node adds 2 new configuration fields

- `IsRoot` -> A root node is a node that starts its own navigation stack, this means that a root node is a node that has its own undo/redo and can only be exited by either saving or exiting from the action bar
- `IsStackable` -> A stackable node is a node that can be pushed to the current active navigation stack. If you navigate from a node that isn't stackable, you can't hit the back button to go back to it. (it's not breadcrumbed and treated as a leaf node)

### CustomizerAnimator

The main animator that handles the transitions between nodes and customizer states.

### NavBar

A scrollable nav bar, the main way of navigating between nodes in the customizer.

### ActionBar

The main way for users to save, exit, undo and redo their changes.

### ScrollingItemPicker

The item picker is a scrollable view of generic items. It makes the following assumptions:

- Items are selectable
- Items have 2 states, initialized and non initialized
- Items will have a way to show that they're processing when they're clicked (async operations)

#### Functionality

- Handle concurrency between button clicks and cancellation tokens
- Handle initialization of item picker cells
- Handle pooling item picker cells
- Handle async operations (initialization, item clicking, etc...)

#### Components

##### IItemPickerDataSource

- The data source is how we communicate between controllers and the item picker
- Provides the following:
  - The size of the element at index
  - The total count of elements to show
  - The logic when an item is clicked at index
  - The logic for initializing the view of an item at index
  - The prefab to use for viewing the item at index
  - The configuration of the layout (grid cellsize, padding, spacing, etc..)

#### Dependencies

- OptimizedScroller (Part of the UI framework)
  - Handles only showing visible elements in the view port

#### Notes

- The item picker makes no assumptions on layout, it works with any layout.

## Navigation Logic

When the customizer navigates through nodes, it keeps track of which ones were visited in what's called `NavigationStack` each navigation stack 
has it's own undo/redo stack, a new navigation stack is created if the `NavigationNode` has the `IsRoot` option. 

### Saving/Exiting

When the user decides to save or exit, we traverse the current navigation stack nodes, if any of the nodes has a custom save or exit action, we interrupt and prompt the user to save or
exit their current customization, if the user successfully completes the action, we exit the node and go back to the previous one. If no nodes in the navigation stack have any custom saving/exiting logic, we simply exit the navigation stack and go back to the previous one.
If we were on the root navigation stack, the customizer will simply dispatch a save or exit event to any listeners and they can handle showing a final exit or discard prompt to the user to exit the whole customizer flow.


## UML

![CustomizerUml.png](images%2FCustomizerUml.png)

