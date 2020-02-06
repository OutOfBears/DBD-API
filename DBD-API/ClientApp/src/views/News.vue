<template>
  <div class="content-container">
    <a-alert
      v-if="error !== ''"
      message="Error loading news..."
      :description="error"
      type="error"
    />
    <div class="row">
      <h1>News</h1>
      <a-select defaultValue="live" style="width: 150px" @change="switchBranch" :disabled="loading">
        <a-select-option value="live">Live</a-select-option>
        <a-select-option value="ptb">Player Test Build</a-select-option>
        <a-select-option value="qa">QA</a-select-option>
        <a-select-option value="stage">Stage</a-select-option>
        <a-select-option value="cert">Cert</a-select-option>
        <a-select-option value="dev">Developer</a-select-option>
      </a-select>
    </div>
    <a-list :data-source="news" :loading="loading">
      <a-list-item class="news-item" slot="renderItem" slot-scope="item, index">
        <div v-if="item.imagePath !== ''" class="image-container">
          <img :src="getIcon(item.imagePath || '')" />
        </div>
        <span class="title">
          <span v-html="item.title"></span>
          <a-tag v-if="item.type === 2" color="red">New Content</a-tag>
          <a-tag v-if="item.type === 3" color="blue">Patch Notes</a-tag>
          <a-tag v-if="item.type === 4" color="purple">Dev Message</a-tag>
          <a-tag v-if="item.type === 5" color="green">New Event</a-tag>
          <a-tag class="darken">{{(new Date(item.startDate)).toLocaleDateString()}}</a-tag>
        </span>
        <span class="body" v-html="fixLinks(item.description)"></span>
      </a-list-item>
    </a-list>
  </div>
</template>

<script>
  import ApiService from "../services/ApiService";

  export default {
    name: "News",

    data() {
      return {
        news: [],
        loading: false,
        branch: "live",
        error: "",
      }
    },


    methods: {
      getIcon(url) {
        return ApiService.getIconUrl("live", url);
        // return ApiService.getIconUrl(this.branch, url);
      },

      fixLinks(source) {
        return source.replace(/\"event\:(.*?)\"/gm, (text) => `"${text.substr(7)} target="_blank" rel="noopener noreferrer"`);
      },

      fetchNews() {
        this.error = "";
        this.loading = true;

        ApiService.getDbdNews(this.branch)
          .then(data => {
            this.loading = false;
            this.news = data;
          })
          .catch(ex => {
            this.loading = false;
            this.error = ex.toString();
            console.warn("WARNING, failed to fetch news: ", ex);
          })
      },
      switchBranch(e){
        if(this.branch === e)
          return;

        this.branch = e;
        this.fetchNews();
      }
    },

    mounted() {
      this.fetchNews();
    }
  }
</script>

<style scoped lang="scss">
  div.content-container {
    div.row {
      display: flex;
      flex-direction: row;
      align-items: baseline;
      justify-content: space-between;
    }

    span.title > div.ant-tag {
      float: right;

      &:not(:last-child){
        margin-right: 0;
      }

      &.darken {
        background: rgba(0,0,0,0.3);
        border-color: rgba(0,0,0,0.3);
        color: #fff;
      }
    }
  }

</style>