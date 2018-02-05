namespace SurviveCore {
    
    public class Settings {

        private bool wireframe;
        private bool fog;
        private bool ambientocclusion;
        private bool physics;
        private bool updatecamera;

        public Settings() {
            wireframe = false;
            fog = true;
            ambientocclusion = true;
            physics = true;
            updatecamera = true;
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
        
    }
    
}