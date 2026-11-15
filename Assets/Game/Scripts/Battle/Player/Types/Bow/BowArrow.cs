using UnityEngine;

namespace MonsterAction.Battle.Player
{
    public class BowArrow : MonoBehaviour
    {
        [SerializeField] private float speed = 25f;       // 矢のスピード
        [SerializeField] private float lifeTime = 5f;     // 消滅までの時間（秒）

        private Vector3 direction;
        private float damage;
        private GameObject owner;
        private bool flinch;

        // プレイヤーから発射されたときに呼ばれる初期化メソッド
        public void Shoot(Vector3 dir, float dmg, GameObject shooter, bool isFlinch = false)
        {
            direction = dir.normalized;
            damage = dmg;
            owner = shooter;
            flinch = isFlinch;

            // 矢の向きを進行方向に向ける
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            // 一定時間後に自動で消滅させる（メモリ節約）
            Destroy(gameObject, lifeTime);
        }

        private void Update()
        {
            // 毎フレーム前方に進む
            transform.position += direction * speed * Time.deltaTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            // 自分自身（プレイヤー）には当たらないようにする
            if (other.gameObject == owner) return;

            // 【メモ】敵に当たったときのダメージ処理（プロジェクトの敵の仕様に合わせて呼び出す）
            // var enemy = other.GetComponent<EnemyHealth>();
            // if (enemy != null) { enemy.TakeDamage(damage, flinch); }

            // 何かに当たったら矢を消滅させる
            Destroy(gameObject);
        }
    }
}