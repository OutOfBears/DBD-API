<template>
  <div class="addons-list-container">
    <a-list itemLayout="horizontal" :dataSource="addons" :loading="loading">
      <a-list-item slot="renderItem" slot-scope="item" v-if="item.iconPathList[0] !== 'UI/Icons/Customization/Missing.png'">
        <div class="addons">
          <div class="icon">
            <img :class="`item-rarity-${getItemRarityIndex(item)}`"
                 :src="getIcon(item.iconPathList[0])" />
          </div>
          <div class="name">
            {{item.displayName}}
          </div>
          <div class="desc">
            <span v-html="item.description"></span>
          </div>
        </div>
      </a-list-item>
    </a-list>
  </div>
</template>

<script>
  import ApiService from "../services/ApiService";

  export default {
    name: "AddonsList",
    props: {
      branch: String,
      loading: Boolean,
      addons: Array
    },
    methods: {
      getIcon(url) {
        if(typeof url !== 'string' || url === '')
          return '';
        return ApiService.getIconUrl(this.branch, url);
      },
      getItemRarityIndex(item) {
        console.log("hi", item);
        let rarity = item.rarity || item.Rarity;
        if (typeof rarity !== 'string')
          return 0;

        switch (rarity) {
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
      }
    }
  }
</script>

<style scoped lang="scss">
  div.addons-list-container {
    width: 100%;

    div.ant-list-item {
      border-bottom: 1px solid rgba(255,255,255,0.05);

      &:last-child {
        border-bottom: 0;
      }
    }

    div.addons {
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
          /*background-size: 170px;*/
          background-size: cover;
          background-repeat: no-repeat;
          background-position: center;

          &.item-rarity-0 {
            background-image: url(../assets/items/common.png);
          }
          &.item-rarity-1 {
            background-image: url(../assets/items/uncommon.png);
          }
          &.item-rarity-2 {
            background-image: url(../assets/items/rare.png);
          }
          &.item-rarity-3 {
            background-image: url(../assets/items/veryrare.png);
          }
          &.item-rarity-4 {
            background-image: url(../assets/items/ultrarare.png);
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

        span.FlavorText {
          font-style: italic;
        }
      }
    }
  }
</style>