namespace beyondnations.desktop.render {

    /**
    * Where the camera sits relative to the player.
    *
    * The Unity build only ever had the third-person view: a camera parented to
    * the player at a fixed local offset. First person (#173) was filed as a
    * feature request against that arrangement and would have meant a second
    * camera rig. Here it is a mode on the one camera, so #173 came down to a
    * key binding, a call to toggleMode and leaving the player out of the
    * snapshot while the eye is on top of them, rather than a new subsystem.
    */
    public enum CameraMode {
        ThirdPerson = 0,
        FirstPerson = 1
    }
}
