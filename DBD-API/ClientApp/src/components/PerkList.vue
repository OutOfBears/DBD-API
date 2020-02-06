<template>
  <div class="perk-list-container">
    <a-list itemLayout="horizontal" :dataSource="formattedPerksList" :loading="loading">
      <a-list-item slot="renderItem" slot-scope="item, index">
        <div class="perk">
          <div class="icon">
            <img :class="`perk-rarity-${getPerkRarityIndex(item)}`"
                 :src="getIcon(item.iconPathList[0])" />
          </div>
          <div class="name">
            {{item.displayName}}
          </div>
          <div class="desc">
            <span class="teachable" v-if="showUnlocks && item.teachableOnBloodWebLevel">
              (This perk is teachable at level {{item.teachableOnBloodWebLevel}})
            </span>
            <span v-html="getPerkDescription(item)"></span>
          </div>
        </div>
      </a-list-item>
    </a-list>
  </div>
</template>

<script>
  import ApiService from "../services/ApiService";

  export default {
    name: "PerkList",
    props: {
      branch: String,
      loading: Boolean,
      showUnlocks: {
        type: Boolean,
        default: false,
      },
      perks: Array
    },
    computed: {
      formattedPerksList: function(){
        return this.perks.sort((x, y) => {
          return x.associatedPlayerIndex - y.associatedPlayerIndex;
        });
      }
    },
    methods: {
      getIcon(url) {
        if(typeof url !== 'string' || url === '')
          return '';
        return ApiService.getIconUrl(this.branch, url);
      },
      getPerkDescription(perk) {
        let desc = '';

        if(perk.perkDefaultDescription !== '') desc = perk.perkDefaultDescription;
        if(perk.perkLevel1Description !== '') desc = perk.perkLevel1Description;
        if(perk.perkLevel2Description !== '') desc = perk.perkLevel2Description;
        if(perk.perkLevel3Description !== '') desc = perk.perkLevel3Description;

        desc = desc.replace(/{\d+}/gm, (rawIdx) => {
          let index = rawIdx.substr(1, rawIdx.length - 2);
          let formatted = "";
          let lastTunable = '';

          for(let i = 0; i < perk.perkLevelRarity.length; i++) {
            let tunable = perk.perkLevelTunables[i][index];
            if(lastTunable === tunable)
              continue;

            lastTunable = tunable;
            formatted = `${formatted}<span class="perk-level-${i}">${tunable}</span>/`;
          }

          formatted = formatted.endsWith('/') ? formatted.substr(0, formatted.length - 1) : formatted;
          return `<span class="perk-formatted">${formatted}</span>`;
        });

        return desc;
      },
      getPerkRarityIndex(perk) {
        let rarity = perk.perkLevelRarity[perk.perkLevelRarity.length - 1];
        if(typeof rarity !== 'string') return 0;
        switch(rarity) {
          default:
            return 0;
          case "EItemRarity::Rare":
            return 1;
          case "EItemRarity::VeryRare":
            return 2;
        }
      }
    }
  }
</script>

<style scoped lang="scss">
  div.perk-list-container {
    width: 100%;

    div.ant-list-item {
      border-bottom: 1px solid rgba(255,255,255,0.05);

      &:last-child {
        border-bottom: 0;
      }
    }

    div.perk {
      flex: 1 1 100%;
      display: flex;
      flex-direction: row;
      color: white;

      span {
        color: inherit;
      }

      div {
        display: flex;
        align-items: center;
        justify-content: center;
      }

      & > div.icon {
        width: 20%;

        & > img {
          background-size: cover;
          background-repeat: no-repeat;
          background-position: center;

          &.perk-rarity-0 {
            background-image: url(../assets/perks/uncommon.png);
          }
          &.perk-rarity-1 {
            background-image: url(../assets/perks/rare.png);
          }
          &.perk-rarity-2 {
            background-image: url(../assets/perks/veryrare.png);
          }
        }
      }

      & > div.name {
        width: 20%;
        font-weight: 700;
      }

      & > div.desc {
        width: 60%;
        padding: 20px;
        display: flex;
        flex-direction: column;

        span.teachable {
          display: block;
          width: 100%;
          font-style: italic;
          text-align: left;
          margin-bottom: 10px;
          color: orangered;
        }
      }
    }
  }
</style>