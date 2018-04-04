namespace SurviveCore {

    public class Settings {

        private static Settings settings;
        public static Settings Instance => settings ?? (settings = new Settings());

        private bool wireframe;
        private bool fog;
        private bool ambientocclusion;
        private bool physics;
        private bool updatecamera;
        private bool debuginfo;
        private bool vsync;
        private bool fullscreen;

        public Settings() {
            wireframe = false;
            fog = true;
            ambientocclusion = true;
            physics = true;
            updatecamera = true;
            debuginfo = false;
            vsync = true;
            fullscreen = false;
        }

        public bool Fullscreen {
            get => fullscreen;
            set => fullscreen = value;
        }

        public bool VSync {
            get => vsync;
            set => vsync = value;
        }

        public bool DebugInfo {
            get => debuginfo;
            set => debuginfo = value;
        }

        public bool Wireframe {
            get => wireframe;
            set => wireframe = value;
        }

        public bool Fog {
            get => fog;
            set => fog = value;
        }

        public bool AmbientOcclusion {
            get => ambientocclusion;
            set => ambientocclusion = value;
        }

        public bool Physics {
            get => physics;
            set => physics = value;
        }

        public bool UpdateCamera {
            get => updatecamera;
            set => updatecamera = value;
        }

        public void ToggleDebugInfo() {
            debuginfo = !debuginfo;
        }
        
        public void ToggleWireframe() {
            wireframe = !wireframe;
        }

        public void ToggleFog() {
            fog = !fog;
        }

        public void ToggleAmbientOcclusion() {
            ambientocclusion = !ambientocclusion;
        }

        public void TogglePhysics() {
            physics = !physics;
        }

        public void ToggleUpdateCamera() {
            updatecamera = !updatecamera;
        }

        public void ToggleVSync() {
            vsync = !vsync;
        }

        public void ToggleFullscreen() {
            fullscreen = !fullscreen;
        }
        
    }
    
}