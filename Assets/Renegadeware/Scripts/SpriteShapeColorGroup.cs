using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    public class SpriteShapeColorGroup : MonoBehaviour {
        public enum Type {
            Override,
            Multiply,
            Add
        }

        public Type type = Type.Multiply;
        public bool initOnAwake = true;

        public UnityEngine.U2D.SpriteShapeRenderer[] spriteShapeRenders;

        public Color color {
            get { return mColor; }
            set {
                if(mColor != value || !mIsApplied) {
                    ApplyColor(value);
                }
            }
        }

        private Color[] mGraphicDefaultColors;

        private bool mIsApplied = false;
        private Color mColor = Color.white;

        public void ApplyColor(Color color) {
            if(spriteShapeRenders == null || spriteShapeRenders.Length == 0 || (mGraphicDefaultColors != null && spriteShapeRenders.Length != mGraphicDefaultColors.Length))
                Init();
            else if(mGraphicDefaultColors == null)
                InitDefaultData();

            switch(type) {
                case Type.Override:
                    for(int i = 0; i < spriteShapeRenders.Length; i++) {
                        if(spriteShapeRenders[i])
                            spriteShapeRenders[i].color = color;
                    }
                    break;
                case Type.Multiply:
                    for(int i = 0; i < spriteShapeRenders.Length; i++) {
                        if(spriteShapeRenders[i])
                            spriteShapeRenders[i].color = mGraphicDefaultColors[i] * color;
                    }
                    break;
                case Type.Add:
                    for(int i = 0; i < spriteShapeRenders.Length; i++) {
                        if(spriteShapeRenders[i])
                            spriteShapeRenders[i].color = new Color(
                                Mathf.Clamp01(mGraphicDefaultColors[i].r + color.r),
                                Mathf.Clamp01(mGraphicDefaultColors[i].g + color.g),
                                Mathf.Clamp01(mGraphicDefaultColors[i].b + color.b),
                                Mathf.Clamp01(mGraphicDefaultColors[i].a + color.a));
                    }
                    break;
            }

            mIsApplied = true;
            mColor = color;
        }

        public void Revert() {
            if(mIsApplied) {
                mIsApplied = false;

                if(spriteShapeRenders == null || mGraphicDefaultColors == null)
                    return;

                for(int i = 0; i < spriteShapeRenders.Length; i++) {
                    if(spriteShapeRenders[i])
                        spriteShapeRenders[i].color = mGraphicDefaultColors[i];
                }

                mColor = Color.white;
            }
        }

        public void Init() {
            Revert();

            spriteShapeRenders = GetComponentsInChildren<UnityEngine.U2D.SpriteShapeRenderer>(true);

            InitDefaultData();
        }

        void OnDestroy() {
            Revert();
        }

        void Awake() {
            if(initOnAwake && (spriteShapeRenders == null || spriteShapeRenders.Length == 0))
                Init();
        }

        private void InitDefaultData() {
            mGraphicDefaultColors = new Color[spriteShapeRenders.Length];

            for(int i = 0; i < spriteShapeRenders.Length; i++)
                mGraphicDefaultColors[i] = spriteShapeRenders[i].color;
        }
    }
}