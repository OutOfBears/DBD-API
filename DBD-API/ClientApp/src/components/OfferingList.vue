<template>
  <div class="offering-list-container">
    <a-list itemLayout="horizontal" :dataSource="formattedOfferingList" :loading="loading">
      <a-list-item slot="renderItem" slot-scope="item, index">
        <div class="offering">
          <div class="icon">
            <img :class="useBackgrounds ? `offering-rarity-${getOfferingRarityIndex(item)}` : ''"
                 :src="getIcon(item.iconPathList[0])" />
          </div>
          <div class="name">
            {{item.displayName}}
          </div>
          <span class="desc" v-html="item.description"></span>
        </div>
      </a-list-item>
    </a-list>
  </div>
</template>

<script>
  import ApiService from "../services/ApiService";

  export default {
    name: "OfferingList",
    props: {
      branch: String,
      loading: Boolean,
      offerings: Array,
      useBackgrounds: {
        type: Boolean,
        default: true
      }
    },
    computed: {
      formattedOfferingList: function(){
        return this.offerings.sort((x, y) => {
          return x.displayName.localeCompare(y.displayName);
        });
      }
    },
    methods: {
      getOfferingRarityIndex(offering){
        let rarity = offering.rarity;
        if(typeof rarity !== 'string')
          return 0;

        switch(rarity){
          default:
            return 0;
          case "EItemRarity::Uncommon":
            return 1;
          case "EItemRarity::Rare":
            return 2;
          case "EItemRarity::VeryRare":
            return 3;
          case "EItemRarity::UltraRare":
            return 4;
        }
      },
      getIcon(url) {
        if(typeof url !== 'string' || url === '')
          return '';

        return ApiService.getIconUrl(this.branch, url);
      }
    }
  }
</script>

<style scoped lang="scss">
  div.offering-list-container {
    width: 100%;

    div.ant-list-item {
      border-bottom: 1px solid rgba(255,255,255,0.05);

      &:last-child {
        border-bottom: 0;
      }
    }

    div.offering {
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

        & img {
          background-size: 170px;
          background-repeat: no-repeat;
          background-position: center;

          &.offering-rarity-0 {
            background-image: url(../assets/offerings/common.png);
          }
          &.offering-rarity-1 {
            background-image: url(../assets/offerings/uncommon.png);
          }
          &.offering-rarity-2 {
            background-image: url(../assets/offerings/rare.png);
          }
          &.offering-rarity-3 {
            background-image: url(../assets/offerings/veryrare.png);
          }
          &.offering-rarity-4 {
            background-image: url(../assets/offerings/ultrarare.png);
          }
        }
      }
      & > div.name {
        width: 20%;
        font-weight: 700;
      }
      & > span.desc {
        width: 60%;
        padding: 20px;
      }
    }
  }
</style>