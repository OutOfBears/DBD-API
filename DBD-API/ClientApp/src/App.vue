<template>
  <div class="app">
    <div class="nav-bar container">
      <a-menu mode="horizontal" :selectedKeys="[$route.name || 'news']">
        <a-menu-item class="logo" key="stats-logo">
          <img src="./assets/logo.png" />
          DBD Stats
        </a-menu-item>
        <a-menu-item key="news">
          <router-link :to="{name: 'news'}">News</router-link>
        </a-menu-item>
        <a-menu-item key="characters">
          <router-link :to="{name: 'characters'}">Characters</router-link>
        </a-menu-item>
        <a-menu-item key="offerings">
          <router-link :to="{name: 'offerings'}">Offerings</router-link>
        </a-menu-item>
        <a-menu-item key="perks">
          <router-link :to="{name: 'perks'}">Perks</router-link>
        </a-menu-item>
        <a-menu-item key="shrine">
          <router-link :to="{name: 'shrine'}">Shrine</router-link>
        </a-menu-item>
        <a-menu-item key="stats" disabled>
          <router-link :to="{name: 'news'}">Player Stats</router-link>
        </a-menu-item>
      </a-menu>
    </div>
    <div class="content">
      <router-view />
    </div>
    <div class="footer container">

    </div>
  </div>
</template>

<script>
  export default {
    name: "app",

    watch: {
      '$route' (to, from) {
        document.title = 'DBD Stats | ' + to.name.replace(/\w+/g,
          (txt) => txt.charAt(0).toUpperCase() + txt.substr(1).toLowerCase());
      }
    },

  }
</script>

<style scoped lang="scss">
  div.app {
    height: 100%;
    width: 100%;
    display: flex;
    flex-direction: column;
    background: #23272A;
    color: white;

    & > div {
      width: 100%;
      display: flex;
      flex-direction: row;


      &.nav-bar > .ant-menu {
        width: 100%;
        background: transparent;
        border: 0;
        color: inherit;
        font-weight: 600;

        & > .ant-menu-item-disabled {
          color: rgba(255,255,255,0.2) !important;

          & > a {
            color: inherit !important;
          }
        }

        & > .ant-menu-item {
          padding: 0;
          position: relative;
          user-select: none;

          &.logo {
            vertical-align: center;
            text-align: center;
            pointer-events: none;
            cursor: auto;

            border-bottom: 2px solid transparent;
            /*width: 50px;*/
            margin-left: 30px;
            margin-right: 20px;

            & > img {
              height: 30px;
              margin-right: 5px;
            }
          }

          &:active {
            background: transparent;
          }

          & > a {
            padding: 0 20px;
            color: inherit;
            width: 100%;
          }
        }
      }

      &.content {
        background: #2C2F33;
        height: 100%;
        overflow: auto;
      }

      &:not(.content){
        /*height: 60px;*/
      }
    }

  }
</style>

<!-- Global Styles -->
<style lang="scss">
  @import './assets/main.scss';
</style>