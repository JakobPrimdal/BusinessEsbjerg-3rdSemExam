/* eslint-disable */
/* tslint:disable */
// @ts-nocheck
/*
 * ---------------------------------------------------------------
 * ## THIS FILE WAS GENERATED VIA SWAGGER-TYPESCRIPT-API        ##
 * ##                                                           ##
 * ## AUTHOR: acacode                                           ##
 * ## SOURCE: https://github.com/acacode/swagger-typescript-api ##
 * ---------------------------------------------------------------
 */

export enum UserRole {
  User = 0,
  Accountant = 1,
  Admin = 2,
}

export interface ActivityLogPageDTO {
  items?: ActivityLogDTO[];
  /** @format int32 */
  totalCount?: number;
  /** @format int32 */
  page?: number;
  /** @format int32 */
  pageSize?: number;
}

export interface ActivityLogDTO {
  /** @format guid */
  id?: string;
  userId?: string | null;
  username?: string;
  action?: string;
  details?: string;
  /** @format date-time */
  createdAt?: string;
}

export interface ActivityLogUserDTO {
  userId?: string;
  username?: string;
}

export interface UserDTO {
  userId?: string;
  username?: string;
  role?: RoleDTO;
  isActive?: boolean;
}

export interface RoleDTO {
  id?: UserRole;
  name?: string;
}

export interface RegisterRequestDTO {
  username?: string;
  password?: string;
  role?: string;
}

export interface LoginRequestDTO {
  username?: string;
  password?: string;
}

export interface HolidayBalanceDTO {
  userId?: string;
  /** @format decimal */
  weeks1To5Hours?: number;
  /** @format decimal */
  week6Hours?: number;
  accounts?: HolidayAccountDTO[];
}

export interface HolidayAccountDTO {
  /** @format guid */
  id?: string;
  holidayType?: string;
  /** @format date */
  earningStart?: string;
  /** @format date */
  earningEnd?: string;
  /** @format date */
  usageStart?: string;
  /** @format date */
  usageEnd?: string;
  /** @format date */
  transferDeadline?: string;
  status?: string;
  /** @format decimal */
  balanceHours?: number;
  months?: MonthlyHolidayDTO[];
}

export interface MonthlyHolidayDTO {
  /** @format date */
  month?: string;
  /** @format decimal */
  hours?: number;
  /** @format decimal */
  weeklyHours?: number | null;
}

export interface CreateWorkHourDTO {
  userId?: string;
  /** @format date */
  validFrom?: string;
  /** @format decimal */
  monHours?: number;
  /** @format decimal */
  tueHours?: number;
  /** @format decimal */
  wedHours?: number;
  /** @format decimal */
  thuHours?: number;
  /** @format decimal */
  friHours?: number;
  /** @format decimal */
  satHours?: number;
  /** @format decimal */
  sunHours?: number;
}

export interface HolidayRequestPreviewDTO {
  /** @format int32 */
  workingDays?: number;
  /** @format decimal */
  hours?: number;
  /** @format decimal */
  balanceBefore?: number;
  /** @format decimal */
  balanceAfter?: number;
  /** @format decimal */
  maxDebt?: number;
  canRequest?: boolean;
  error?: string | null;
  shortNotice?: boolean;
  /** @format int32 */
  noticeMonths?: number;
}

export interface CreateHolidayRequestDTO {
  /** @format date */
  startDate?: string;
  /** @format date */
  endDate?: string;
  holidayType?: string;
}

export interface HolidayRequestDTO {
  /** @format guid */
  id?: string;
  holidayType?: string;
  /** @format date */
  startDate?: string;
  /** @format date */
  endDate?: string;
  /** @format decimal */
  hours?: number;
  status?: string;
  /** @format date-time */
  createdAt?: string;
  shortNotice?: boolean;
  /** @format int32 */
  noticeMonths?: number;
}

export interface PendingHolidayRequestDTO {
  /** @format guid */
  id?: string;
  userId?: string;
  username?: string;
  holidayType?: string;
  /** @format date */
  startDate?: string;
  /** @format date */
  endDate?: string;
  /** @format decimal */
  hours?: number;
  /** @format date-time */
  createdAt?: string;
  /** @format decimal */
  balanceBefore?: number;
  /** @format decimal */
  balanceAfter?: number;
  problem?: string | null;
}

export interface HolidayDecisionDTO {
  /** @format guid */
  id?: string;
  status?: string;
  decidedBy?: string;
  /** @format date-time */
  decidedAt?: string;
  decisionReason?: string | null;
}

export interface DenyHolidayRequestDTO {
  reason?: string | null;
}

export interface MyHolidayRequestDTO {
  /** @format guid */
  id?: string;
  holidayType?: string;
  /** @format date */
  startDate?: string;
  /** @format date */
  endDate?: string;
  /** @format decimal */
  hours?: number;
  status?: string;
  /** @format date-time */
  createdAt?: string;
  decisionReason?: string | null;
  cancellationDecisionReason?: string | null;
  canCancel?: boolean;
  cancelNeedsApproval?: boolean;
}

export interface HolidayCancellationDTO {
  /** @format guid */
  id?: string;
  status?: string;
  hoursReturned?: boolean;
  /** @format decimal */
  hours?: number;
}

export interface CancellationRequestDTO {
  /** @format guid */
  id?: string;
  userId?: string;
  username?: string;
  holidayType?: string;
  /** @format date */
  startDate?: string;
  /** @format date */
  endDate?: string;
  /** @format decimal */
  hours?: number;
  /** @format date-time */
  cancellationRequestedAt?: string;
}

export interface CancellationDecisionDTO {
  /** @format guid */
  id?: string;
  status?: string;
  decidedBy?: string;
  /** @format date-time */
  decidedAt?: string;
  reason?: string | null;
  /** @format decimal */
  hoursReturned?: number;
}

export interface DenyCancellationDTO {
  reason?: string | null;
}

export interface HolidayRuleSetResponseDTO {
  /** @format guid */
  id?: string;
  /** @format date */
  validFrom?: string;
  createdBy?: string | null;
  /** @format date-time */
  createdAt?: string;
  isActive?: boolean;
  /** @format decimal */
  weeks1To5EarningRate?: number;
  /** @format decimal */
  week6EarningRate?: number;
  /** @format int32 */
  weeks1To5EarningStartDay?: number;
  /** @format int32 */
  weeks1To5EarningStartMonth?: number;
  /** @format int32 */
  weeks1To5EarningEndDay?: number;
  /** @format int32 */
  weeks1To5EarningEndMonth?: number;
  /** @format int32 */
  weeks1To5EarningEndYearOffset?: number;
  /** @format int32 */
  week6EarningStartDay?: number;
  /** @format int32 */
  week6EarningStartMonth?: number;
  /** @format int32 */
  week6EarningEndDay?: number;
  /** @format int32 */
  week6EarningEndMonth?: number;
  /** @format int32 */
  week6EarningEndYearOffset?: number;
  /** @format int32 */
  weeks1To5UsageStartDay?: number;
  /** @format int32 */
  weeks1To5UsageStartMonth?: number;
  /** @format int32 */
  weeks1To5UsageStartYearOffset?: number;
  /** @format int32 */
  weeks1To5UsageEndDay?: number;
  /** @format int32 */
  weeks1To5UsageEndMonth?: number;
  /** @format int32 */
  weeks1To5UsageEndYearOffset?: number;
  /** @format int32 */
  week6UsageStartDay?: number;
  /** @format int32 */
  week6UsageStartMonth?: number;
  /** @format int32 */
  week6UsageStartYearOffset?: number;
  /** @format int32 */
  week6UsageEndDay?: number;
  /** @format int32 */
  week6UsageEndMonth?: number;
  /** @format int32 */
  week6UsageEndYearOffset?: number;
  /** @format int32 */
  mainHolidayStartDay?: number;
  /** @format int32 */
  mainHolidayStartMonth?: number;
  /** @format int32 */
  mainHolidayEndDay?: number;
  /** @format int32 */
  mainHolidayEndMonth?: number;
  holidayTransferEnabled?: boolean;
  /** @format int32 */
  maximumTransferDays?: number;
  /** @format int32 */
  weeks1To5TransferDeadlineDay?: number;
  /** @format int32 */
  weeks1To5TransferDeadlineMonth?: number;
  /** @format int32 */
  weeks1To5TransferDeadlineYearOffset?: number;
  /** @format int32 */
  week6TransferDeadlineDay?: number;
  /** @format int32 */
  week6TransferDeadlineMonth?: number;
  /** @format int32 */
  week6TransferDeadlineYearOffset?: number;
  /** @format int32 */
  sickWaitingDays?: number;
  /** @format int32 */
  mainHolidayNoticePeriodMonths?: number;
  /** @format int32 */
  otherHolidayNoticePeriodMonths?: number;
}

export interface HolidayRuleSetRequestDTO {
  /** @format date */
  validFrom?: string;
  isActive?: boolean;
  /** @format decimal */
  weeks1To5EarningRate?: number;
  /** @format decimal */
  week6EarningRate?: number;
  /** @format int32 */
  weeks1To5EarningStartDay?: number;
  /** @format int32 */
  weeks1To5EarningStartMonth?: number;
  /** @format int32 */
  weeks1To5EarningEndDay?: number;
  /** @format int32 */
  weeks1To5EarningEndMonth?: number;
  /** @format int32 */
  weeks1To5EarningEndYearOffset?: number;
  /** @format int32 */
  week6EarningStartDay?: number;
  /** @format int32 */
  week6EarningStartMonth?: number;
  /** @format int32 */
  week6EarningEndDay?: number;
  /** @format int32 */
  week6EarningEndMonth?: number;
  /** @format int32 */
  week6EarningEndYearOffset?: number;
  /** @format int32 */
  weeks1To5UsageStartDay?: number;
  /** @format int32 */
  weeks1To5UsageStartMonth?: number;
  /** @format int32 */
  weeks1To5UsageStartYearOffset?: number;
  /** @format int32 */
  weeks1To5UsageEndDay?: number;
  /** @format int32 */
  weeks1To5UsageEndMonth?: number;
  /** @format int32 */
  weeks1To5UsageEndYearOffset?: number;
  /** @format int32 */
  week6UsageStartDay?: number;
  /** @format int32 */
  week6UsageStartMonth?: number;
  /** @format int32 */
  week6UsageStartYearOffset?: number;
  /** @format int32 */
  week6UsageEndDay?: number;
  /** @format int32 */
  week6UsageEndMonth?: number;
  /** @format int32 */
  week6UsageEndYearOffset?: number;
  /** @format int32 */
  mainHolidayStartDay?: number;
  /** @format int32 */
  mainHolidayStartMonth?: number;
  /** @format int32 */
  mainHolidayEndDay?: number;
  /** @format int32 */
  mainHolidayEndMonth?: number;
  holidayTransferEnabled?: boolean;
  /** @format int32 */
  maximumTransferDays?: number;
  /** @format int32 */
  weeks1To5TransferDeadlineDay?: number;
  /** @format int32 */
  weeks1To5TransferDeadlineMonth?: number;
  /** @format int32 */
  weeks1To5TransferDeadlineYearOffset?: number;
  /** @format int32 */
  week6TransferDeadlineDay?: number;
  /** @format int32 */
  week6TransferDeadlineMonth?: number;
  /** @format int32 */
  week6TransferDeadlineYearOffset?: number;
  /** @format int32 */
  sickWaitingDays?: number;
  /** @format int32 */
  mainHolidayNoticePeriodMonths?: number;
  /** @format int32 */
  otherHolidayNoticePeriodMonths?: number;
}

export type QueryParamsType = Record<string | number, any>;
export type ResponseFormat = keyof Omit<Body, "body" | "bodyUsed">;

export interface FullRequestParams extends Omit<RequestInit, "body"> {
  /** set parameter to `true` for call `securityWorker` for this request */
  secure?: boolean;
  /** request path */
  path: string;
  /** content type of request body */
  type?: ContentType;
  /** query params */
  query?: QueryParamsType;
  /** format of response (i.e. response.json() -> format: "json") */
  format?: ResponseFormat;
  /** request body */
  body?: unknown;
  /** base url */
  baseUrl?: string;
  /** request cancellation token */
  cancelToken?: CancelToken;
}

export type RequestParams = Omit<
  FullRequestParams,
  "body" | "method" | "query" | "path"
>;

export interface ApiConfig<SecurityDataType = unknown> {
  baseUrl?: string;
  baseApiParams?: Omit<RequestParams, "baseUrl" | "cancelToken" | "signal">;
  securityWorker?: (
    securityData: SecurityDataType | null,
  ) => Promise<RequestParams | void> | RequestParams | void;
  customFetch?: typeof fetch;
}

export interface HttpResponse<D extends unknown, E extends unknown = unknown>
  extends Response {
  data: D;
  error: E;
}

type CancelToken = Symbol | string | number;

export enum ContentType {
  Json = "application/json",
  JsonApi = "application/vnd.api+json",
  FormData = "multipart/form-data",
  UrlEncoded = "application/x-www-form-urlencoded",
  Text = "text/plain",
}

export class HttpClient<SecurityDataType = unknown> {
  public baseUrl: string = "http://localhost:5194";
  private securityData: SecurityDataType | null = null;
  private securityWorker?: ApiConfig<SecurityDataType>["securityWorker"];
  private abortControllers = new Map<CancelToken, AbortController>();
  private customFetch = (...fetchParams: Parameters<typeof fetch>) =>
    fetch(...fetchParams);

  private baseApiParams: RequestParams = {
    credentials: "same-origin",
    headers: {},
    redirect: "follow",
    referrerPolicy: "no-referrer",
  };

  constructor(apiConfig: ApiConfig<SecurityDataType> = {}) {
    Object.assign(this, apiConfig);
  }

  public setSecurityData = (data: SecurityDataType | null) => {
    this.securityData = data;
  };

  protected encodeQueryParam(key: string, value: any) {
    const encodedKey = encodeURIComponent(key);
    return `${encodedKey}=${encodeURIComponent(typeof value === "number" ? value : `${value}`)}`;
  }

  protected addQueryParam(query: QueryParamsType, key: string) {
    return this.encodeQueryParam(key, query[key]);
  }

  protected addArrayQueryParam(query: QueryParamsType, key: string) {
    const value = query[key];
    return value.map((v: any) => this.encodeQueryParam(key, v)).join("&");
  }

  protected toQueryString(rawQuery?: QueryParamsType): string {
    const query = rawQuery || {};
    const keys = Object.keys(query).filter(
      (key) => "undefined" !== typeof query[key],
    );
    return keys
      .map((key) =>
        Array.isArray(query[key])
          ? this.addArrayQueryParam(query, key)
          : this.addQueryParam(query, key),
      )
      .join("&");
  }

  protected addQueryParams(rawQuery?: QueryParamsType): string {
    const queryString = this.toQueryString(rawQuery);
    return queryString ? `?${queryString}` : "";
  }

  private contentFormatters: Record<ContentType, (input: any) => any> = {
    [ContentType.Json]: (input: any) =>
      input !== null && (typeof input === "object" || typeof input === "string")
        ? JSON.stringify(input)
        : input,
    [ContentType.JsonApi]: (input: any) =>
      input !== null && (typeof input === "object" || typeof input === "string")
        ? JSON.stringify(input)
        : input,
    [ContentType.Text]: (input: any) =>
      input !== null && typeof input !== "string"
        ? JSON.stringify(input)
        : input,
    [ContentType.FormData]: (input: any) => {
      if (input instanceof FormData) {
        return input;
      }

      return Object.keys(input || {}).reduce((formData, key) => {
        const property = input[key];
        formData.append(
          key,
          property instanceof Blob
            ? property
            : typeof property === "object" && property !== null
              ? JSON.stringify(property)
              : `${property}`,
        );
        return formData;
      }, new FormData());
    },
    [ContentType.UrlEncoded]: (input: any) => this.toQueryString(input),
  };

  protected mergeRequestParams(
    params1: RequestParams,
    params2?: RequestParams,
  ): RequestParams {
    return {
      ...this.baseApiParams,
      ...params1,
      ...(params2 || {}),
      headers: {
        ...(this.baseApiParams.headers || {}),
        ...(params1.headers || {}),
        ...((params2 && params2.headers) || {}),
      },
    };
  }

  protected createAbortSignal = (
    cancelToken: CancelToken,
  ): AbortSignal | undefined => {
    if (this.abortControllers.has(cancelToken)) {
      const abortController = this.abortControllers.get(cancelToken);
      if (abortController) {
        return abortController.signal;
      }
      return void 0;
    }

    const abortController = new AbortController();
    this.abortControllers.set(cancelToken, abortController);
    return abortController.signal;
  };

  public abortRequest = (cancelToken: CancelToken) => {
    const abortController = this.abortControllers.get(cancelToken);

    if (abortController) {
      abortController.abort();
      this.abortControllers.delete(cancelToken);
    }
  };

  public request = async <T = any, E = any>({
    body,
    secure,
    path,
    type,
    query,
    format,
    baseUrl,
    cancelToken,
    ...params
  }: FullRequestParams): Promise<HttpResponse<T, E>> => {
    const secureParams =
      ((typeof secure === "boolean" ? secure : this.baseApiParams.secure) &&
        this.securityWorker &&
        (await this.securityWorker(this.securityData))) ||
      {};
    const requestParams = this.mergeRequestParams(params, secureParams);
    const queryString = query && this.toQueryString(query);
    const payloadFormatter = this.contentFormatters[type || ContentType.Json];
    const responseFormat = format || requestParams.format;

    return this.customFetch(
      `${baseUrl || this.baseUrl || ""}${path}${queryString ? `?${queryString}` : ""}`,
      {
        ...requestParams,
        headers: {
          ...(requestParams.headers || {}),
          ...(type && type !== ContentType.FormData
            ? { "Content-Type": type }
            : {}),
        },
        signal:
          (cancelToken
            ? this.createAbortSignal(cancelToken)
            : requestParams.signal) || null,
        body:
          typeof body === "undefined" || body === null
            ? null
            : payloadFormatter(body),
      },
    ).then(async (response) => {
      const r = response as HttpResponse<T, E>;
      r.data = null as unknown as T;
      r.error = null as unknown as E;

      const responseToParse = responseFormat ? response.clone() : response;
      const data = !responseFormat
        ? r
        : await responseToParse[responseFormat]()
            .then((data) => {
              if (r.ok) {
                r.data = data;
              } else {
                r.error = data;
              }
              return r;
            })
            .catch((e) => {
              r.error = e;
              return r;
            });

      if (cancelToken) {
        this.abortControllers.delete(cancelToken);
      }

      if (!response.ok) throw data;
      return data;
    });
  };
}

/**
 * @title My Title
 * @version 1.0.0
 * @baseUrl http://localhost:5194
 */
export class Api<
  SecurityDataType extends unknown,
> extends HttpClient<SecurityDataType> {
  activityLog = {
    /**
     * No description
     *
     * @tags ActivityLog
     * @name ActivityLogGetActivityLog
     * @request GET:/activitylog/GetActivityLog
     * @secure
     */
    activityLogGetActivityLog: (
      query?: {
        userId?: string | null;
        /** @format date-time */
        from?: string | null;
        /** @format date-time */
        to?: string | null;
        /**
         * @format int32
         * @default 1
         */
        page?: number;
      },
      params: RequestParams = {},
    ) =>
      this.request<ActivityLogPageDTO, any>({
        path: `/activitylog/GetActivityLog`,
        method: "GET",
        query: query,
        secure: true,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags ActivityLog
     * @name ActivityLogGetUsers
     * @request GET:/activitylog/GetUsers
     * @secure
     */
    activityLogGetUsers: (params: RequestParams = {}) =>
      this.request<ActivityLogUserDTO[], any>({
        path: `/activitylog/GetUsers`,
        method: "GET",
        secure: true,
        format: "json",
        ...params,
      }),
  };
  auth = {
    /**
     * No description
     *
     * @tags Auth
     * @name AuthRegister
     * @request POST:/Register
     * @secure
     */
    authRegister: (data: RegisterRequestDTO, params: RequestParams = {}) =>
      this.request<UserDTO, any>({
        path: `/Register`,
        method: "POST",
        body: data,
        secure: true,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Auth
     * @name AuthLogin
     * @request POST:/Login
     */
    authLogin: (data: LoginRequestDTO, params: RequestParams = {}) =>
      this.request<UserDTO, any>({
        path: `/Login`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Auth
     * @name AuthLogout
     * @request POST:/Logout
     */
    authLogout: (params: RequestParams = {}) =>
      this.request<Blob, any>({
        path: `/Logout`,
        method: "POST",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Auth
     * @name AuthGetMe
     * @request GET:/GetMe
     * @secure
     */
    authGetMe: (params: RequestParams = {}) =>
      this.request<UserDTO, any>({
        path: `/GetMe`,
        method: "GET",
        secure: true,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Auth
     * @name AuthAdminCheck
     * @request GET:/AdminCheck
     * @secure
     */
    authAdminCheck: (params: RequestParams = {}) =>
      this.request<string, any>({
        path: `/AdminCheck`,
        method: "GET",
        secure: true,
        format: "json",
        ...params,
      }),
  };
  holiday = {
    /**
     * No description
     *
     * @tags Holiday
     * @name HolidayGetMyBalance
     * @request GET:/holiday/GetMyBalance
     * @secure
     */
    holidayGetMyBalance: (params: RequestParams = {}) =>
      this.request<HolidayBalanceDTO, any>({
        path: `/holiday/GetMyBalance`,
        method: "GET",
        secure: true,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Holiday
     * @name HolidayGetBalance
     * @request GET:/holiday/GetBalance/{userId}
     * @secure
     */
    holidayGetBalance: (userId: string, params: RequestParams = {}) =>
      this.request<HolidayBalanceDTO, any>({
        path: `/holiday/GetBalance/${userId}`,
        method: "GET",
        secure: true,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Holiday
     * @name HolidayAddWorkHours
     * @request POST:/holiday/AddWorkHours
     * @secure
     */
    holidayAddWorkHours: (
      data: CreateWorkHourDTO,
      params: RequestParams = {},
    ) =>
      this.request<Blob, any>({
        path: `/holiday/AddWorkHours`,
        method: "POST",
        body: data,
        secure: true,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * No description
     *
     * @tags Holiday
     * @name HolidayAddMonth
     * @request POST:/holiday/AddMonth
     * @secure
     */
    holidayAddMonth: (
      query?: {
        /** @format date */
        month?: string;
      },
      params: RequestParams = {},
    ) =>
      this.request<Blob, any>({
        path: `/holiday/AddMonth`,
        method: "POST",
        query: query,
        secure: true,
        ...params,
      }),

    /**
     * No description
     *
     * @tags Holiday
     * @name HolidayPreviewRequest
     * @request POST:/holiday/PreviewRequest
     * @secure
     */
    holidayPreviewRequest: (
      data: CreateHolidayRequestDTO,
      params: RequestParams = {},
    ) =>
      this.request<HolidayRequestPreviewDTO, any>({
        path: `/holiday/PreviewRequest`,
        method: "POST",
        body: data,
        secure: true,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Holiday
     * @name HolidayRequestHoliday
     * @request POST:/holiday/RequestHoliday
     * @secure
     */
    holidayRequestHoliday: (
      data: CreateHolidayRequestDTO,
      params: RequestParams = {},
    ) =>
      this.request<HolidayRequestDTO, any>({
        path: `/holiday/RequestHoliday`,
        method: "POST",
        body: data,
        secure: true,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Holiday
     * @name HolidayGetPendingRequests
     * @request GET:/holiday/GetPendingRequests
     * @secure
     */
    holidayGetPendingRequests: (params: RequestParams = {}) =>
      this.request<PendingHolidayRequestDTO[], any>({
        path: `/holiday/GetPendingRequests`,
        method: "GET",
        secure: true,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Holiday
     * @name HolidayApproveRequest
     * @request POST:/holiday/ApproveRequest/{requestId}
     * @secure
     */
    holidayApproveRequest: (requestId: string, params: RequestParams = {}) =>
      this.request<HolidayDecisionDTO, any>({
        path: `/holiday/ApproveRequest/${requestId}`,
        method: "POST",
        secure: true,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Holiday
     * @name HolidayDenyRequest
     * @request POST:/holiday/DenyRequest/{requestId}
     * @secure
     */
    holidayDenyRequest: (
      requestId: string,
      data: DenyHolidayRequestDTO,
      params: RequestParams = {},
    ) =>
      this.request<HolidayDecisionDTO, any>({
        path: `/holiday/DenyRequest/${requestId}`,
        method: "POST",
        body: data,
        secure: true,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Holiday
     * @name HolidayGetMyRequests
     * @request GET:/holiday/GetMyRequests
     * @secure
     */
    holidayGetMyRequests: (params: RequestParams = {}) =>
      this.request<MyHolidayRequestDTO[], any>({
        path: `/holiday/GetMyRequests`,
        method: "GET",
        secure: true,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Holiday
     * @name HolidayCancelRequest
     * @request POST:/holiday/CancelRequest/{requestId}
     * @secure
     */
    holidayCancelRequest: (requestId: string, params: RequestParams = {}) =>
      this.request<HolidayCancellationDTO, any>({
        path: `/holiday/CancelRequest/${requestId}`,
        method: "POST",
        secure: true,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Holiday
     * @name HolidayGetCancellationRequests
     * @request GET:/holiday/GetCancellationRequests
     * @secure
     */
    holidayGetCancellationRequests: (params: RequestParams = {}) =>
      this.request<CancellationRequestDTO[], any>({
        path: `/holiday/GetCancellationRequests`,
        method: "GET",
        secure: true,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Holiday
     * @name HolidayApproveCancellation
     * @request POST:/holiday/ApproveCancellation/{requestId}
     * @secure
     */
    holidayApproveCancellation: (
      requestId: string,
      params: RequestParams = {},
    ) =>
      this.request<CancellationDecisionDTO, any>({
        path: `/holiday/ApproveCancellation/${requestId}`,
        method: "POST",
        secure: true,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Holiday
     * @name HolidayDenyCancellation
     * @request POST:/holiday/DenyCancellation/{requestId}
     * @secure
     */
    holidayDenyCancellation: (
      requestId: string,
      data: DenyCancellationDTO,
      params: RequestParams = {},
    ) =>
      this.request<CancellationDecisionDTO, any>({
        path: `/holiday/DenyCancellation/${requestId}`,
        method: "POST",
        body: data,
        secure: true,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),
  };
  holidayRuleSet = {
    /**
     * No description
     *
     * @tags HolidayRuleSet
     * @name HolidayRuleSetGetHolidayRuleSets
     * @request GET:/GetHolidayRuleSets
     * @secure
     */
    holidayRuleSetGetHolidayRuleSets: (params: RequestParams = {}) =>
      this.request<HolidayRuleSetResponseDTO[], any>({
        path: `/GetHolidayRuleSets`,
        method: "GET",
        secure: true,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags HolidayRuleSet
     * @name HolidayRuleSetGetCurrent
     * @request GET:/GetCurrent
     * @secure
     */
    holidayRuleSetGetCurrent: (params: RequestParams = {}) =>
      this.request<HolidayRuleSetResponseDTO, any>({
        path: `/GetCurrent`,
        method: "GET",
        secure: true,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags HolidayRuleSet
     * @name HolidayRuleSetGetHolidayRuleSetByDate
     * @request GET:/GetHolidayRuleSetByDate
     * @secure
     */
    holidayRuleSetGetHolidayRuleSetByDate: (
      query?: {
        /** @format date */
        date?: string;
      },
      params: RequestParams = {},
    ) =>
      this.request<HolidayRuleSetResponseDTO, any>({
        path: `/GetHolidayRuleSetByDate`,
        method: "GET",
        query: query,
        secure: true,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags HolidayRuleSet
     * @name HolidayRuleSetCreateHolidayRuleSet
     * @request POST:/CreateHolidayRuleSet
     * @secure
     */
    holidayRuleSetCreateHolidayRuleSet: (
      data: HolidayRuleSetRequestDTO,
      params: RequestParams = {},
    ) =>
      this.request<HolidayRuleSetResponseDTO, any>({
        path: `/CreateHolidayRuleSet`,
        method: "POST",
        body: data,
        secure: true,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags HolidayRuleSet
     * @name HolidayRuleSetToggleActive
     * @request POST:/ToggleActive
     * @secure
     */
    holidayRuleSetToggleActive: (
      query?: {
        /** @format guid */
        id?: string;
      },
      params: RequestParams = {},
    ) =>
      this.request<HolidayRuleSetResponseDTO, any>({
        path: `/ToggleActive`,
        method: "POST",
        query: query,
        secure: true,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags HolidayRuleSet
     * @name HolidayRuleSetGetRulesById
     * @request GET:/GetRulesById
     * @secure
     */
    holidayRuleSetGetRulesById: (
      query?: {
        /** @format guid */
        id?: string;
      },
      params: RequestParams = {},
    ) =>
      this.request<HolidayRuleSetResponseDTO, any>({
        path: `/GetRulesById`,
        method: "GET",
        query: query,
        secure: true,
        format: "json",
        ...params,
      }),
  };
}
