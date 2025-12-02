namespace Genies.Customization.Framework
{
    public enum CustomizerViewLayer
    {
        //Layer for customization controls views above the nar bar/below if using drawer style
        CustomizationEditor,

        //Layer for popups, overlays, etc... (screen takeover in general)
        PopupsAndOverlays,

        //Layer for editors that are above everything else but below the action bar
        CustomizationEditorFullScreen,

        //Layer for views at the top right of the drawer
        DrawerTopRight,

        //Layer for views at the top left of the drawer
        DrawerTopLeft
    }
}
