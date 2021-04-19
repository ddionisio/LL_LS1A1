using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Renegadeware.LL_LS1A1 {
    [CreateAssetMenu(fileName = "component", menuName = "Game/Organism/Component/Generic")]
    public class OrganismComponentGeneric : OrganismComponent {
        [System.Serializable]
        public struct BodySprite {
            [OrganismComponentID]
            public int bodyID;

            public Sprite sprite;

            public void Apply(SpriteRenderer renderer) {
                if(sprite)
                    renderer.sprite = sprite;
            }
        }

        [Header("Prefabs")]
        [SerializeField]
        GameObject _editPrefab;
        [SerializeField]
        GameObject _gamePrefab;

        [Header("Body")]
        public BodySprite[] bodySprites;

        public Color bodyEditColor = Color.white;

        [SerializeField]
        string _anchor;

        public override string anchorName => _anchor;

        public override GameObject editPrefab => _editPrefab;
        public override GameObject gamePrefab => _gamePrefab;

        public override void SetupTemplate(OrganismEntity organismEntity) {
            //apply sprite
            if(organismEntity.bodyDisplay.spriteRender) {
                var bodyID = organismEntity.bodyComponent.ID;

                for(int i = 0; i < bodySprites.Length; i++) {
                    var bodySprite = bodySprites[i];
                    if(bodySprite.bodyID == bodyID) {
                        bodySprite.Apply(organismEntity.bodyDisplay.spriteRender);
                        break;
                    }
                }
            }
        }

        public override void SetupEditBody(OrganismDisplayBody displayBody) {
            if(displayBody.colorGroup)
                displayBody.colorGroup.ApplyColor(bodyEditColor);
        }
    }
}