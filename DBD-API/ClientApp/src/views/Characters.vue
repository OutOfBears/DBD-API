<template>
  <div class="content-container characters-container">
    <a-alert
        v-if="error !== ''"
        message="Error loading news..."
        :description="error"
        type="error"
    />

    <h1>Survivors</h1>
    <a-list :grid="{ gutter: 16, xs: 1, sm: 2, md: 4, lg: 4, xl: 6, xxl: 4 }" :dataSource="characters.filter(x => x.characterIndex >= 0 && x.characterIndex < 268435456)" :loading="this.loading">
      <a-list-item slot="renderItem" slot-scope="item">
        <router-link class="characterItem" :to="'/characters/' + item.characterIndex">
          <img :src="getIcon(item.iconPath)" />
          <span>{{item.displayName}}</span>
        </router-link>
      </a-list-item>
    </a-list>
    <h1>Killers</h1>
    <a-list :grid="{ gutter: 16, xs: 1, sm: 2, md: 4, lg: 4, xl: 6, xxl: 4 }" :dataSource="characters.filter(x => x.characterIndex >= 268435456)" :loading="this.loading">
      <a-list-item slot="renderItem" slot-scope="item">
        <router-link class="characterItem" :to="'/characters/' + item.characterIndex">
          <img :src="getIcon(item.iconPath)" />
          <span>{{item.displayName}}</span>
        </router-link>
      </a-list-item>
    </a-list>
  </div>
</template>

<script>
  import ApiService from "../services/ApiService";

  export default {
    name: "Characters",

    data() {
      return {
        error: "",
        branch: "live",
        characters: [],
        loading: false,
      }
    },

    methods: {
      getIcon(url) {
        return ApiService.getIconUrl(this.branch, url);
      },

      updateCharacters() {
        this.loading = true;
        ApiService.getCharacters(this.branch)
          .then(data => {
            this.characters = Object.values(data);
            this.loading = false;
          })
          .catch(ex => {
            console.warn("WARNING failed to fetch characters:", ex);
            this.error = ex.toString();
            this.loading = false;
          })
      }
    },

    mounted(){
      this.updateCharacters();
    }
  }
</script>

<style scoped lang="scss">
  div.characters-container {
    .characterItem {
      display: flex;
      flex-direction: column;

      background: rgba(0,0,0,0.4);
      border: 1px solid rgba(0,0,0,0.4);
      border-radius: 3px;
      cursor: pointer;

      display: flex;
      align-items: center;
      justify-content: center;

      transition: all .2s ease-in;

      &:hover {
        border-color: #1890ff;

        & > span {
          transition: all .2s ease-in;
          color: rgba(255,255,255,1);
        }
      }

      & > img {
        height: 140px;
        width: 140px;
      }

      & > span {
        color: rgba(255,255,255,0.6);
        font-size: .8em;
        font-weight: 700;
        padding: 5px;
      }
    }
  }
</style>