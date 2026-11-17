using MonsterAction.Combat;
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

            // 1. ぶつかった相手から「IDamageable」コンポーネント（ダメージを受けられる仕組み）を取得する
            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                // 2. 資料に沿った DamageInfo を作成する
                // 引数: (ダメージ量, 当たった場所, 攻撃の向き, 攻撃者, 怯ませるか, ノックバック距離, ダウンか)
                Vector3 hitPoint = other.ClosestPoint(transform.position);
                Vector3 hitDirection = transform.forward;

                DamageInfo info = new DamageInfo(
                    amount: damage,
                    hitPoint: hitPoint,
                    direction: hitDirection,
                    attacker: owner != null ? owner : gameObject,
                    causesFlinch: flinch,
                    knockbackDistance: 1.0f, // 必要に応じて調整
                    knockDown: false
                );

                // 3. 敵側の ApplyDamage メソッドを呼び出してダメージを伝える
                damageable.ApplyDamage(info);
            }

            // 何かに当たったら矢を消滅させる
            Destroy(gameObject);
        }
    }
}