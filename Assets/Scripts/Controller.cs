using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
https://toen.itch.io/toens-medieval-strategy
https://github.com/Brackeys/Health-Bar
https://opengameart.org/content/greyarms
https://opengameart.org/content/knife-sharpening-slice-1
https://opengameart.org/content/knife-sharpening-slice-2
*/

[System.Serializable]
public class CounterEntity {
    public SpriteRenderer entityCounter0, entityCounter1, entityCounter2;

    public void SetValue(Sprite[] counters, int count) {
        entityCounter0.sprite = counters[count % 10];
        entityCounter1.enabled = count > 9;        
        entityCounter1.sprite = counters[(count % 100) / 10];
        entityCounter2.enabled = count > 99;
        entityCounter2.sprite = counters[(count % 1000) / 100];
    }
}

[System.Serializable]
public class PersonEntity {
    public SpriteRenderer entity, selector, entityCounter0, entityCounter1, entityCounter2;
    public UnityEngine.UI.Button button;
    public GameObject birthingProgress;
    public float birthingTime;
}

public class Controller : MonoBehaviour
{
    public UnityEngine.Tilemaps.Tilemap peopleTilemap;
    public PersonEntity farmerEntity, guardEntity, minerEntity;
    public GameObject minerDecayProgress;
    public float minerDecayTime;
    public GameObject knightPrefab;
    public Sprite[] counters;
    public Color disabledColor;
    public float knightInvadeTimeDelay, knightInvadeTimeMin, knightInvadeTimeMax, knightTimeScale, knightSpeed;
    public CounterEntity sparkleCounter, defenseCounter;
    public GameObject deathOverlay;
    public SpriteRenderer sparkler;
    public AudioSource knife1, knife2;

    private bool deadState;
    private float elapsedTime;
    private float lastInputTime;
    private float lastSparkleTime;
    private float nextBirthTime;
    private float nextInvadeTime;
    private float nextMinerDecayTime;
    private int farmerCount, guardCount, minerCount;
    private List<SpriteRenderer> knights;
    private int selectedEntity;
    private int sparkles, defenses;

    // Start is called before the first frame update
    void Start()
    {
        deadState = false;
        deathOverlay.SetActive(false);
        elapsedTime = 0;
        lastInputTime = 0;
        lastSparkleTime = 0;
        selectedEntity = 0;
        nextBirthTime = farmerEntity.birthingTime;
        nextMinerDecayTime = minerDecayTime;
        farmerCount = 1;
        guardCount = 1;
        minerCount = 2;
        sparkles = 0;
        defenses = 0;
        nextInvadeTime = knightInvadeTimeDelay;
        knights = new List<SpriteRenderer>();
        farmerEntity.button.onClick.AddListener(OnClickFarmer);
        guardEntity.button.onClick.AddListener(OnClickGuard);
        minerEntity.button.onClick.AddListener(OnClickMiner);
        sparkler.enabled = false;
        UpdateEntities();
    }

    void OnClickFarmer() {
        SelectEntity(0);
    }

    void OnClickGuard() {
        SelectEntity(1);
    }

    void OnClickMiner() {
        SelectEntity(2);
    }

    private void SelectEntity(int i) {
        if (selectedEntity == i) {
            return;
        }
        selectedEntity = i;
        PersonEntity entity;
        switch (selectedEntity) {
            case 2: entity = minerEntity; break;
            case 1: entity = guardEntity; break;
            default: entity = farmerEntity; break;
        }
        nextBirthTime = elapsedTime + entity.birthingTime;
    }

    private void UpdateEntity(PersonEntity personEntity, int count, bool active) {
        float scaleSize;
        if (active) {
            personEntity.selector.color = Color.white;
            scaleSize = Mathf.Repeat(elapsedTime, 1.0f) > 0.5 ? 1.1f : 1.2f;
        } else {
            personEntity.selector.color = new Color(0.5f, 0.5f, 0.5f, 0.25f);
            scaleSize = 1.1f;
        }
        float scalePos = ((scaleSize - 1) / 2);
        personEntity.selector.transform.localScale = new Vector3(scaleSize, scaleSize, 1);
        personEntity.selector.transform.localPosition = new Vector3(-scalePos, scalePos, 0);
        
        personEntity.birthingProgress.SetActive(active);
        if (active) {
            UnityEngine.UI.Slider slider = personEntity.birthingProgress.GetComponent<UnityEngine.UI.Slider>();
            slider.value = 1 - ((nextBirthTime - elapsedTime) / personEntity.birthingTime);
        }

        if (count < 0) {
            count = 0;
        }
        personEntity.entity.color = count < 1 ? disabledColor : Color.white;
        personEntity.entityCounter0.sprite = counters[count % 10];
        personEntity.entityCounter1.enabled = count > 9;        
        personEntity.entityCounter1.sprite = counters[(count % 100) / 10];
        personEntity.entityCounter2.enabled = count > 99;
        personEntity.entityCounter2.sprite = counters[(count % 1000) / 100];
    }

    private void UpdateEntities() {
        UpdateEntity(farmerEntity, farmerCount, selectedEntity == 0);
        UpdateEntity(guardEntity, guardCount, selectedEntity == 1);
        UpdateEntity(minerEntity, minerCount, selectedEntity == 2);
    }

    private void GetInput() {
        if (elapsedTime - lastInputTime < 0.5) {
            return;
        }
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        if (Input.GetKeyDown("left") || Input.GetKeyDown("a")) {
            horizontal = -1;
        }
        if (Input.GetKeyDown("right") || Input.GetKeyDown("d")) {
            horizontal = 1;
        }
        if (Input.GetKeyDown("down") || Input.GetKeyDown("s")) {
            vertical = -1;
        }
        if (Input.GetKeyDown("up") || Input.GetKeyDown("w")) {
            vertical = 1;
        }
        if (Mathf.Abs(horizontal) > 0.1) {
            switch (selectedEntity) {
                case 2: SelectEntity(horizontal > 0 ? 1 : 0); break;
                case 1: SelectEntity(horizontal > 0 ? 0 : 2); break;
                default: SelectEntity(horizontal > 0 ? 2 : 1); break;
            }
            lastInputTime = elapsedTime;
        } else if (Mathf.Abs(vertical) > 0.1) {
            switch (selectedEntity) {
                case 2: SelectEntity(vertical > 0 ? 0 : 1); break;
                case 1: SelectEntity(vertical > 0 ? 2 : 0); break;
                default: SelectEntity(vertical > 0 ? 1 : 2); break;
            }
            lastInputTime = elapsedTime;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (deadState) {
            selectedEntity = -1;
            UpdateEntities();
            deathOverlay.SetActive(true);
            GameObject.Find("Music").GetComponent<AudioSource>().Stop();
            return;
        }
        elapsedTime += Time.deltaTime;
        GetInput();
        while (elapsedTime >= nextInvadeTime) {
            float minTime = Mathf.Max(0.1f, knightInvadeTimeMin * (1.0f / (float)(Mathf.Max(elapsedTime, knightTimeScale) / knightTimeScale)));
            float maxTime = Mathf.Max(0.1f, knightInvadeTimeMax * (1.0f / (float)(Mathf.Max(elapsedTime, knightTimeScale) / knightTimeScale)));
            nextInvadeTime += Random.Range(minTime, maxTime);
            knights.Add(Instantiate(knightPrefab).GetComponent<SpriteRenderer>());
        }
        foreach (SpriteRenderer knight in knights) {
            Transform trs = knight.gameObject.transform;
            if (trs.position.x > 3) {
                trs.Translate(-knightSpeed, 0, 0);
            } else if (trs.position.y > 0.5) {
                if (guardCount > 0) {
                    guardCount -= 1;
                    guardEntity.entity.transform.localPosition = new Vector3(0.1f, 0, 2.0f);
                    defenses += 1;
                    knife1.Play();
                    knight.enabled = false;
                } else {
                    trs.Translate(0, -knightSpeed, 0);
                }
            } else if (trs.position.y > -1) {
                trs.Translate(0, -knightSpeed, 0);
            } else if (trs.position.x > -4) {
                trs.Translate(-knightSpeed, 0, 0);
            } else {
                farmerCount -= 5;
                if (farmerCount <= 0) {
                    deadState = true;
                }
                knife2.Play();
                knight.enabled = false;
            }
        }
        for (int i = 0; i < knights.Count; ) {
            if (!knights[i].enabled) {
                Destroy(knights[i].gameObject);
                knights.RemoveAt(i);
            } else {
                i++;
            }
        }
        while (elapsedTime >= nextBirthTime) {
            switch (selectedEntity) {
                case 2: minerCount += 1; nextBirthTime += minerEntity.birthingTime; break;
                case 1: guardCount += 1; nextBirthTime += guardEntity.birthingTime; break;
                default: farmerCount += 1; nextBirthTime += farmerEntity.birthingTime; break;
            }
        }
        while (elapsedTime >= nextMinerDecayTime) {
            minerCount -= 1;
            nextMinerDecayTime += minerDecayTime;
        }
        if (minerCount < 1) {
            lastSparkleTime = elapsedTime;
            nextMinerDecayTime = elapsedTime + minerDecayTime;
            minerDecayProgress.GetComponent<UnityEngine.UI.Slider>().value = 0;
            minerEntity.entity.transform.position = new Vector3(-2, 4, 0);
        } else {
            float sparkleInterval = 2.0f / ((float)Mathf.Clamp(minerCount, 0, 10));
            while (elapsedTime >= lastSparkleTime + sparkleInterval) {
                sparkles += 1;
                lastSparkleTime = lastSparkleTime + sparkleInterval;
                sparkler.enabled = true;
                Vector3 tmp = sparkler.transform.position;
                tmp.z = 2.0f;
                sparkler.transform.position = tmp;
            }
            minerDecayProgress.GetComponent<UnityEngine.UI.Slider>().value = ((nextMinerDecayTime - elapsedTime) / minerDecayTime);
            minerEntity.entity.transform.localPosition = new Vector3(Mathf.Repeat(elapsedTime - lastSparkleTime, sparkleInterval) > (sparkleInterval / 2.0f) ? 0f : 0.1f, 0, 0);
        }
        if (sparkler.transform.position.z > 0.0f) {
            Vector3 tmp = sparkler.transform.position;
            tmp.z = Mathf.Max(0.0f, tmp.z - 0.1f);
            if (tmp.z <= 0.001) {
                tmp.z = 0;
                sparkler.enabled = false;
            }
            sparkler.transform.position = tmp;
        }
        UpdateEntities();
        if (guardEntity.entity.transform.localPosition.z > 0.0f) {
            float z = Mathf.Max(0.0f, guardEntity.entity.transform.localPosition.z - 0.1f);
            if (z <= 0.001) {
                guardEntity.entity.transform.localPosition = new Vector3(0, 0, 0);
            } else {
                guardEntity.entity.transform.localPosition = new Vector3(0.1f, 0, z);
            }
        }
        sparkleCounter.SetValue(counters, sparkles);
        defenseCounter.SetValue(counters, defenses);
    }
}
