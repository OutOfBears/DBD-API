<template>
  <div class="content-container">
    <div class="row">
      <h1>Shrine Of Secrets</h1>
      <p>Resets in {{shrineTime}}</p>
    </div>
    <div class="shrine-perks">
      <div class="shrine-container">
        <div v-for="(item, idx) in shrine.items"
             v-if="typeof perks[item.id] === 'object'"
             :key="idx"
             :class="`perk${idx}`"
             @mouseover="item.hover = true"
             @mouseleave="item.hover = false">
          <div class="hover-over" v-if="item.hover">

          </div>

          <img :src="getIcon((perks[item.id].iconPathList || [])[0])" />
        </div>
      </div>
    </div>
  </div>
</template>

<script>
  import ApiService from "../services/ApiService";

  const INVALID_DATE = (function(){
    let date = new Date();
    date.setTime(0);
    return date;
  })();

  export default {
    name: "Shrine",

    data() {
      return {
        error: "",
        branch: "live",
        loading: false,
        shrineTime: '00:00:00',
        countDown: INVALID_DATE,
        perks: { },
        shrine: {
          StartDate: INVALID_DATE,
          EndDate: INVALID_DATE,
          Items: [],
          Week: 0
        }
      }
    },

    mounted() {
      ApiService.getPerks(this.branch)
        .then(data => {
          this.perks = data;
          this.fetchShrine();
          console.log(this.perks);
        })
        .catch(ex =>
        {
          console.warn("WARNING failed to fetch perks:", ex);
          this.error = ex.toString()
        });

      this.interval = setInterval(() => {
        if(this.countDown !== null) {
            let exp = this.$moment(this.countDown, 'DD.MM.YYYY HH:mm:ss');
            this.shrineTime = this.formatDuration(this.$moment.duration(exp.diff(this.$moment())));
            this.$forceUpdate();
         }
      }, 1000);
    },

    beforeDestroy(){
      clearInterval(this.interval);
    },

    methods: {
      getIcon(url) {
        if(typeof url !== 'string' || url === '')
          return '';

        return ApiService.getIconUrl(this.branch, url);
      },
      fetchShrine() {
        this.loading = true;
        ApiService.getShrine(this.branch)
          .then(data => {
            this.loading = false;
            this.shrine = {
              ...data,
              startDate: new Date(data.startDate),
              endDate: new Date(data.endDate),
            };
            this.countDown = this.shrine.endDate;
            console.log("shrine", this.shrine);
          })
          .catch(ex => {
            this.loading = false;
            this.error = ex.toString();
            console.warn("WARNING failed to fetch shrine:", ex);
          })
      },
      formatDuration(tsDuration) {
        let before = '';
        let days = Math.floor(tsDuration.asDays());
        if(days > 0) before = `${days} days, `;

        return before + this.$moment({ ...tsDuration._data })
          .format('HH:mm:ss');
      }
    }
  }
</script>

<style scoped lang="scss">
  div.content-container {
    height: calc(100% - 40px);
    width: 100%;
    position: relative;

    & > div.row {
      display: flex;
      flex-direction: row;
      align-items: baseline;
      justify-content: space-between;
      margin-bottom: 20px;

      & > * {
        margin: 0;
      }

      & p {
        color: rgba(255,255,255,0.6);
        font-weight: 600;
      }
    }


    & > div.shrine-perks {
      display: flex;
      align-items: center;
      justify-content: center;
      margin-top: 40px;
      margin-bottom: calc((256px * 2) + 25px);

      & > div.shrine-container {
        position: relative;

        & > div {
          position: absolute;
          background-image: url(../assets/perks/teachable.png);
          background-size: cover;

          & > div.hover-over {
            position: absolute;
            left: 0;
            top: 0;
            width: 100%;
            height: 100%;
            background: rgba(0,0,0,0.5);
          }

          & > img {

          }
        }

        & > div.perk0 {
          top: 50%;
          left: 50%;
          transform: translate(-105%, 55%);
        }

        & > div.perk1 {
          left: 50%;
          transform: translateX(-50%);
        }

        & > div.perk2 {
          top: 50%;
          right: 50%;
          transform: translate(105%, 55%);
        }

        & > div.perk3 {
          left: 50%;
          transform: translate(-50%, 110%);
        }
      }
    }
  }
</style>